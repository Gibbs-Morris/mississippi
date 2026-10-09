#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR issue reference validator' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/validate-pr-issue-reference.ps1'
        $mergeScriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/validate-merge-group-pr-issue-reference.ps1'
        $knownIssues = @(
            [pscustomobject]@{ number = 741; title = 'Coverage binding'; state = 'open' }
            [pscustomobject]@{ number = 742; title = 'Worktree lease'; state = 'closed'; type = 'issue' }
            [pscustomobject]@{ number = 743; title = 'Native command bounds'; state = 'open'; type = 'pull_request' }
        ) | ConvertTo-Json -Compress

        function Invoke-ReferenceValidator {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $output = & $powerShellPath -NoProfile -File $scriptPath -Body $Body -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $knownIssues -Json 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $output | ConvertFrom-Json; Output = $output }
        }

        function Invoke-MergeGroupValidator {
            param([AllowEmptyCollection()][object[]]$PullRequests)
            $pullRequestsJson = if (@($PullRequests).Count -eq 0) { '[]' } else { ConvertTo-Json -InputObject @($PullRequests) -Depth 10 -Compress }
            $output = & $powerShellPath -NoProfile -File $mergeScriptPath -PullRequestsJson $pullRequestsJson -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $knownIssues 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
        }

        function Assert-ValidReferenceBody {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $outcome = Invoke-ReferenceValidator -Body $Body
            $outcome.ExitCode | Should -Be 0
            $outcome.Result.Valid | Should -BeTrue
            return $outcome
        }

        function Assert-NoReferenceBody {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $outcome = Invoke-ReferenceValidator -Body $Body
            $outcome.ExitCode | Should -Not -Be 0
            $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
            return $outcome
        }
    }

    It 'accepts a local shorthand and same-repository issue URL' {
        $outcome = Assert-ValidReferenceBody -Body 'Refs #741 and https://github.com/Gibbs-Morris/mississippi/issues/741.'
        @($outcome.Result.ResolvedIssues).Count | Should -Be 1
    }

    It 'preserves GitHub-visible Markdown text consumed by advanced extensions: <Case>' -ForEach @(
        @{ Case = 'abbreviation definition'; Body = '*[term]: Refs #741' }
        @{ Case = 'generic attribute syntax'; Body = 'Context {#741}' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores abbreviation-like text inside code or external links: <Case>' -ForEach @(
        @{ Case = 'inline code'; Body = '`*[term]: Refs #741`' }
        @{ Case = 'external anchor'; Body = '<a href="https://github.com/other/repo/issues/999">*[term]: Refs #741</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'ignores unused footnote definitions: <Case>' -ForEach @(
        @{ Case = 'shorthand'; Body = '[^note]: Refs #741' }
        @{ Case = 'issue URL'; Body = '[^note]: https://github.com/Gibbs-Morris/mississippi/issues/741' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'accepts tracking rendered in a used footnote: <Case>' -ForEach @(
        @{ Case = 'shorthand'; Definition = 'Refs #741' }
        @{ Case = 'issue URL'; Definition = 'https://github.com/Gibbs-Morris/mississippi/issues/741' }
    ) {
        Assert-ValidReferenceBody -Body ('Context[^note]' + [Environment]::NewLine + [Environment]::NewLine + '[^note]: ' + $Definition)
    }

    It 'preserves real tracking while ignoring an unused closed-issue footnote' {
        $outcome = Assert-ValidReferenceBody -Body ('Refs #741' + [Environment]::NewLine + [Environment]::NewLine + '[^note]: Refs #742')
        @($outcome.Result.References).Count | Should -Be 1
    }

    It 'accepts a qualified same-repository issue reference' {
        Assert-ValidReferenceBody -Body 'Refs Gibbs-Morris/mississippi#741.'
    }

    It 'ignores a same-repository pull request link when an issue is present' {
        Assert-ValidReferenceBody -Body 'Refs #741; parent PR: https://github.com/Gibbs-Morris/mississippi/pull/743.'
    }

    It 'accepts a Markdown URI autolink to a same-repository issue' {
        Assert-ValidReferenceBody -Body '<https://github.com/Gibbs-Morris/mississippi/issues/741>'
    }

    It 'rejects a full issue URL with an invalid numeric boundary' {
        Assert-NoReferenceBody -Body 'https://github.com/Gibbs-Morris/mississippi/issues/741abc'
    }

    It 'accepts a used Markdown reference definition' {
        Assert-ValidReferenceBody -Body "[tracking issue][work]`r`n`r`n[work]: https://github.com/Gibbs-Morris/mississippi/issues/741"
    }

    It 'accepts collapsed reference-style issue links' {
        $body = '[tracking issue][]' + [Environment]::NewLine + [Environment]::NewLine + '[tracking issue]: https://github.com/Gibbs-Morris/mississippi/issues/741'
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores balanced Markdown link destinations while scanning shorthand' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/a(b)#741)'
    }

    It 'ignores escaped parentheses inside Markdown link destinations' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/a\)#741)'
    }

    It 'ignores issue URLs nested inside an external URL' {
        Assert-NoReferenceBody -Body 'Context: https://example.test/?next=https://github.com/Gibbs-Morris/mississippi/issues/741'
    }

    It 'does not recover a local shorthand from an external issue URL fragment' {
        Assert-NoReferenceBody -Body 'Context: https://github.com/other/repo/issues/999#741'
    }

    It 'ignores repository issue URLs nested after an external URL fragment' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/#https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores repository issue URLs nested after an external URI colon' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/redirect:https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores repository issue URLs in Markdown link titles' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test "See https://github.com/Gibbs-Morris/mississippi/issues/741.")'
    }

    It 'ignores quoted indented code blocks' {
        Assert-NoReferenceBody -Body ">     Refs #741"
    }

    It 'ignores nested quoted indented code blocks' {
        Assert-NoReferenceBody -Body '> >     Refs #741'
    }

    It 'accepts an issue URL in an ordinary Markdown link' {
        Assert-ValidReferenceBody -Body '[tracking issue](https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'accepts repository-relative issue destinations: <Case>' -ForEach @(
        @{ Case = 'Markdown'; Body = '[Refs #741](/Gibbs-Morris/mississippi/issues/741)' }
        @{ Case = 'HTML'; Body = '<a href="/Gibbs-Morris/mississippi/issues/741">tracking</a>' }
        @{ Case = 'trailing URL whitespace'; Body = '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741 ">tracking</a>' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores relative links outside the local issue destination: <Case>' -ForEach @(
        @{ Case = 'external repository'; Body = '[Refs #741](/other/repo/issues/999)' }
        @{ Case = 'pull request'; Body = '[Refs #741](/Gibbs-Morris/mississippi/pull/741)' }
        @{ Case = 'leading whitespace'; Body = '<a href=" /Gibbs-Morris/mississippi/issues/741">tracking</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'accepts a rendered HTML anchor to a same-repository issue' {
        Assert-ValidReferenceBody -Body '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking issue</a>'
    }

    It 'preserves rendered tracking through select markup: <Case>' -ForEach (@(
        'plain option|<select><option>Refs #741</option></select>'
        'ignored code token|<select><option><code>Refs #741</code></option></select>'
        'discarded external anchor|<select><option><a href="https://github.com/other/repo/issues/999">example</select> Refs #741'
        'ignored external link label|<select><option><a href="https://example.test">Refs #741</a></select>'
        'nested select boundary|<select><select><a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a>'
        'input boundary|<select><input><a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a>'
        'table cell boundary|<table><tr><td><select><option>example</td><td><a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a></td></tr></table>'
    ) | ForEach-Object {
        $caseFields = $_ -split '\|', 2
        @{ Case = $caseFields[0]; Body = $caseFields[1] }
    }) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores discarded select destinations and preserves enclosing ownership: <Case>' -ForEach (@(
        'discarded local destination|<select><option><a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a></select>'
        'enclosing code|<code><select><option>Refs #741</option></select></code>'
        'enclosing external anchor|<a href="https://example.test"><select><option>Refs #741</option></select></a>'
    ) | ForEach-Object {
        $caseFields = $_ -split '\|', 2
        @{ Case = $caseFields[0]; Body = $caseFields[1] }
    }) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves tracking labels when GitHub strips a space-prefixed link: <Case>' -ForEach @(
        @{ Case = 'external URL'; Body = '<a href=" https://example.test ">Refs #741</a>' }
        @{ Case = 'local URL'; Body = '<a href=" https://github.com/Gibbs-Morris/mississippi/issues/741">Refs #741</a>' }
        @{ Case = 'encoded tab'; Body = '<a href="&#9;https://example.test">Refs #741</a>' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'preserves tracking labels when GitHub strips a disallowed href scheme: <Case>' -ForEach (@(
        'JavaScript|javascript:void(0)'
        'data|data:text/html,example'
        'VBScript|vbscript:example'
        'FTP|ftp://example.test'
        'unknown|custom:example'
        'mixed case|JaVaScRiPt:void(0)'
        'encoded tab|java&#9;script:void(0)'
    ) | ForEach-Object {
        $caseFields = $_ -split '\|', 2
        @{ Case = $caseFields[0]; Href = $caseFields[1] }
    }) {
        Assert-ValidReferenceBody -Body ('<a href="' + $Href + '">Refs #741</a>')
    }

    It 'preserves tracking across retained and stripped URL controls: <Case>' -ForEach @(
        @{ Case = 'stripped tab in scheme'; Body = '<a href="htt&#9;ps://example.test">Refs #741</a>' }
        @{ Case = 'stripped newline in scheme'; Body = '<a href="htt&#10;ps://example.test">Refs #741</a>' }
        @{ Case = 'retained carriage return in local path'; Body = '<a href="https://github.com/Gibbs-Morris/missis&#13;sippi/issues/741">tracking</a>' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'rejects stripped or distinct URL-control destinations without tracking: <Case>' -ForEach @(
        @{ Case = 'tab in local scheme'; Body = '<a href="htt&#9;ps://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a>' }
        @{ Case = 'newline in local scheme'; Body = '<a href="htt&#10;ps://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a>' }
        @{ Case = 'percent-encoded path'; Body = '<a href="https://github.com/Gibbs-Morris/missis%0Dsippi/issues/741">tracking</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves allowed scheme and relative link ownership: <Href>' -ForEach @(
        @{ Href = 'mailto:example@example.test' }
        @{ Href = 'xmpp:example@example.test' }
        @{ Href = 'github-windows://example' }
        @{ Href = 'github-mac://example' }
        @{ Href = 'example/javascript:void(0)' }
    ) {
        Assert-NoReferenceBody -Body ('<a href="' + $Href + '">Refs #741</a>')
    }

    It 'rejects stripped links without tracking and preserves valid link ownership: <Case>' -ForEach @(
        @{ Case = 'stripped local URL'; Body = '<a href=" https://github.com/Gibbs-Morris/mississippi/issues/741 ">tracking</a>' }
        @{ Case = 'valid external URL'; Body = '<a href="https://example.test">Refs #741</a>' }
        @{ Case = 'valid empty URL'; Body = '<a href="">Refs #741</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves tracking after rendered HTML comment termination: <Comment>' -ForEach @(
        @{ Comment = '<!-->' }
        @{ Comment = '<!--->' }
        @{ Comment = '<!-- hidden --!>' }
        @{ Comment = '<!-- hidden -->' }
    ) {
        Assert-ValidReferenceBody -Body ($Comment + ' Refs #741')
    }

    It 'ignores hidden or owned tracking around comment endings: <Case>' -ForEach @(
        @{ Case = 'incomplete comment'; Body = '<!-- Refs #741' }
        @{ Case = 'nonterminating exclamation'; Body = '<!--!> Refs #741' }
        @{ Case = 'nonterminating dash'; Body = '<!---!> Refs #741' }
        @{ Case = 'hidden label'; Body = '<!-- Refs #741 --!>' }
        @{ Case = 'enclosing code'; Body = '<code><!--> Refs #741</code>' }
        @{ Case = 'enclosing external link'; Body = '<a href="https://example.test"><!--> Refs #741</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves tracking after a plain processing instruction end: <Case>' -ForEach @(
        @{ Case = 'visible shorthand'; Body = '<?example> Refs #741' }
        @{ Case = 'issue destination'; Body = '<?example> <a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</a>' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores hidden or owned processing instruction text: <Case>' -ForEach @(
        @{ Case = 'unterminated'; Body = '<?example Refs #741' }
        @{ Case = 'enclosing code'; Body = '<code><?example> Refs #741</code>' }
        @{ Case = 'enclosing external link'; Body = '<a href="https://example.test"><?example> Refs #741</a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves visible attributes on GFM-filtered tags: <Tag>' -ForEach @(
        @{ Tag = 'textarea' }
        @{ Tag = 'script' }
        @{ Tag = 'title' }
    ) {
        Assert-ValidReferenceBody -Body ('<' + $Tag + ' title="Refs #741">example</' + $Tag + '>')
    }

    It 'ignores filtered-tag examples owned by code or external links: <Case>' -ForEach @(
        @{ Case = 'inline code'; Body = '`<textarea title="Refs #741">example</textarea>`' }
        @{ Case = 'enclosing external anchor'; Body = '<a href="https://example.test"><textarea title="Refs #741">example</textarea></a>' }
        @{ Case = 'different tag name'; Body = '<textarea-custom title="Refs #741">example</textarea-custom>' }
        @{ Case = 'external anchor inside escaped textarea'; Body = '<textarea><a href="https://github.com/other/repo/issues/999">example</textarea> Refs #741' }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'ignores non-rendered HTML declarations: <Case>' -ForEach @(
        @{ Case = 'document type'; Markup = '<!DOCTYPE html PUBLIC "#741">' }
        @{ Case = 'processing instruction'; Markup = '<?example #741 ?>' }
        @{ Case = 'CDATA section'; Markup = '<![CDATA[Refs #741]]>' }
    ) {
        Assert-NoReferenceBody -Body $Markup
        Assert-ValidReferenceBody -Body ($Markup + [Environment]::NewLine + 'Refs #741')
    }

    It 'preserves a visible HTML declaration example: <Case>' -ForEach @(
        @{ Case = 'encoded'; Body = '&lt;!DOCTYPE html PUBLIC "#741"&gt;' }
        @{ Case = 'escaped by the renderer'; Body = '<!doctype html PUBLIC "#741">' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ends table-local links or code at a scope boundary: <Case>' -ForEach (@(
        'data cell anchor|x <table><tr><td><a href="https://github.com/other/repo/issues/999">context</td><td>Refs #741</td></tr></table>'
        'header cell anchor|x <table><tr><th><a href="https://github.com/other/repo/issues/999">context</th><td>Refs #741</td></tr></table>'
        'implicit cell anchor|x <table><tr><td><a href="https://github.com/other/repo/issues/999">context<td>Refs #741</td></tr></table>'
        'implicit row anchor|x <table><tr><td><a href="https://github.com/other/repo/issues/999">context<tr><td>Refs #741</td></tr></table>'
        'implicit table anchor|x <table><tr><td><a href="https://github.com/other/repo/issues/999">context</table> Refs #741'
        'caption anchor|<table><caption><a href="https://github.com/other/repo/issues/999">context</caption></table> Refs #741'
        'implicit caption row|<table><caption><a href="https://github.com/other/repo/issues/999">context<tr><td>Refs #741</td></tr></table>'
        'implicit caption table|<table><caption><a href="https://github.com/other/repo/issues/999">context</table> Refs #741'
        'cell code|<table><tr><td><code>example</td><td>Refs #741</td></tr></table>'
        'implicit cell code|<table><tr><td><code>example<td>Refs #741</td></tr></table>'
        'preformatted cell|<table><tr><td><pre>example</td><td>Refs #741</td></tr></table>'
        'caption code|<table><caption><code>example</caption><tr><td>Refs #741</td></tr></table>'
    ) | ForEach-Object {
        $caseFields = $_ -split '\|', 2
        @{ Case = $caseFields[0]; Body = $caseFields[1] }
    }) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'preserves enclosing links or code through unmatched table boundaries: <Case>' -ForEach (@(
        'enclosing anchor|<a href="https://github.com/other/repo/issues/999"><table><caption>#741</caption></table> #741</a>'
        'mismatched caption row|<table><caption><a href="https://github.com/other/repo/issues/999">context</tr> #741</caption></table>'
        'caption outside table|<caption><a href="https://github.com/other/repo/issues/999">context</caption> #741</a>'
        'enclosing code|<code><table><tr><td>Refs #741</td></tr></table></code>'
        'mismatched cell|<table><tr><td><code>example</th> #741</code></td></tr></table>'
        'cell outside table|<td><code>example</td> #741</code>'
    ) | ForEach-Object {
        $caseFields = $_ -split '\|', 2
        @{ Case = $caseFields[0]; Body = $caseFields[1] }
    }) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'preserves anchor ownership after a replacement table closes: <Case>' -ForEach @(
        @{ Case = 'direct table'; Prefix = '<table>' }
        @{ Case = 'row'; Prefix = '<table><tr>' }
        @{ Case = 'table body'; Prefix = '<table><tbody>' }
    ) {
        Assert-NoReferenceBody -Body ($Prefix + '<table></table><td><a href="https://github.com/other/repo/issues/999">context</td> Refs #741')
    }

    It 'accepts tracking after an explicitly closed replacement-table anchor' {
        Assert-ValidReferenceBody -Body '<table><table></table><td><a href="https://github.com/other/repo/issues/999">context</td></a> Refs #741'
    }

    It 'restores the enclosing cell anchor after closing a nested table cell' {
        $outcome = Assert-ValidReferenceBody -Body 'x <table><tr><td><a href="https://github.com/other/repo/issues/999">context<table><tr><td>nested</td></tr></table> #742</td><td>Refs #741</td></tr></table>'
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'preserves an external anchor enclosing a table and subsequent text' {
        Assert-NoReferenceBody -Body 'x <a href="https://github.com/other/repo/issues/999"><table><tr><td>#741</td></tr></table> #741</a>'
    }

    It 'ignores a mismatched cell end tag when retaining anchor ownership' {
        Assert-NoReferenceBody -Body 'x <table><tr><td><a href="https://github.com/other/repo/issues/999">context</th> #741</td></tr></table>'
    }

    It 'ignores cell tags outside a table when retaining anchor ownership' {
        Assert-NoReferenceBody -Body 'x <a href="https://github.com/other/repo/issues/999">context</td> #741</a>'
    }

    It 'retains a local issue destination when its anchor ends with the cell' {
        Assert-ValidReferenceBody -Body 'x <table><tr><td><a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking</td></tr></table>'
    }

    It 'ignores HTML issue-looking labels whose destination is not a repository issue: <Case>' -ForEach @(
        @{ Case = 'upstream Dependabot issue'; Anchor = '<a href="https://redirect.github.com/actions/setup-java/issues/999">#999</a>' }
        @{ Case = 'external label matching a local issue'; Anchor = '<a href="https://github.com/other/repo/issues/999">#741</a>' }
        @{ Case = 'nested label markup'; Anchor = '<a href="https://github.com/other/repo/issues/999"><strong>#999</strong></a>' }
        @{ Case = 'same-repository pull request'; Anchor = '<a href="https://github.com/Gibbs-Morris/mississippi/pull/743">#743</a>' }
    ) {
        Assert-NoReferenceBody -Body $Anchor
        $outcome = Assert-ValidReferenceBody -Body ('Refs #741; context: ' + $Anchor)
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'uses an HTML issue destination rather than its numeric label' {
        $outcome = Assert-ValidReferenceBody -Body '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">#999</a>'
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'ignores the numeric label of an unclosed external HTML anchor' {
        Assert-NoReferenceBody -Body '<a href="https://github.com/other/repo/issues/999">#741'
    }

    It 'uses the destination of an unclosed local HTML anchor' {
        $outcome = Assert-ValidReferenceBody -Body '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">#999'
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'ignores closing-anchor text inside quoted child attributes: <Case>' -ForEach @(
        @{ Case = 'double-quoted attribute'; Body = '<a href="https://github.com/other/repo/issues/999"><span title="example </a>">#741</span></a>' }
        @{ Case = 'single-quoted attribute'; Body = '<a href="https://github.com/other/repo/issues/999"><span title=''example </a>''>#741</span></a>' }
    ) {
        Assert-NoReferenceBody -Body $Body
        Assert-ValidReferenceBody -Body ($Body + ' Refs #741')
    }

    It 'ignores a local issue URL embedded in another tag attribute' {
        Assert-NoReferenceBody -Body '<span title=''example <a href="https://github.com/Gibbs-Morris/mississippi/issues/741">''>context</span>'
    }

    It 'preserves tracking after a custom element whose name begins with a' {
        Assert-ValidReferenceBody -Body '<a-widget href="https://example.test">context</a-widget> Refs #741'
    }

    It 'ignores an issue URL in a custom element href attribute' {
        Assert-NoReferenceBody -Body '<a-widget href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a-widget>'
    }

    It 'ends the prior destination when a nested anchor has no href: <Case>' -ForEach @(
        @{ Case = 'bare anchor'; Tag = '<a>' }
        @{ Case = 'anchor with a quoted href decoy'; Tag = '<a title=''href="https://example.test"''>' }
    ) {
        Assert-ValidReferenceBody -Body ('<a href="https://github.com/other/repo/issues/999">context' + $Tag + 'Refs #741</a>')
    }

    It 'keeps an issue-looking label inside a nested external anchor untracked' {
        Assert-NoReferenceBody -Body '<a href="https://example.test">context<a href="https://github.com/other/repo/issues/999">#741</a>'
    }

    It 'uses the destination of a nested local issue anchor' {
        $outcome = Assert-ValidReferenceBody -Body '<a href="https://example.test">context<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">#999</a>'
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'uses a rendered Markdown link inside an external HTML anchor: <Case>' -ForEach @(
        @{ Case = 'inline link'; Body = '<a href="https://example.test">context [tracking](https://github.com/Gibbs-Morris/mississippi/issues/741)</a>' }
        @{ Case = 'URI autolink'; Body = '<a href="https://example.test">context <https://github.com/Gibbs-Morris/mississippi/issues/741></a>' }
        @{ Case = 'reference link'; Body = '<a href="https://example.test">context [tracking][work]</a>' + [Environment]::NewLine + [Environment]::NewLine + '[work]: https://github.com/Gibbs-Morris/mississippi/issues/741' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores an issue-looking label in a nested external Markdown link' {
        Assert-NoReferenceBody -Body '<a href="https://example.test">context [#741](https://github.com/other/repo/issues/999)</a>'
    }

    It 'uses only the actual HTML href attribute: <Case>' -ForEach @(
        @{ Case = 'external href before local data-href'; Expected = $false; Anchor = '<a href="https://example.test" data-href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
        @{ Case = 'data-href without href'; Expected = $false; Anchor = '<a data-href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
        @{ Case = 'local href before external data-href'; Expected = $true; Anchor = '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741" data-href="https://example.test">context</a>' }
        @{ Case = 'local data-href before external href'; Expected = $false; Anchor = '<a data-href="https://github.com/Gibbs-Morris/mississippi/issues/741" href="https://example.test">context</a>' }
        @{ Case = 'external data-href before local href'; Expected = $true; Anchor = '<a data-href="https://example.test" href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
        @{ Case = 'fake local href inside a title'; Expected = $false; Anchor = '<a href="https://example.test" title=''href="https://github.com/Gibbs-Morris/mississippi/issues/741"''>context</a>' }
        @{ Case = 'fake external href inside a title'; Expected = $true; Anchor = '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741" title=''href="https://example.test"''>tracking</a>' }
    ) {
        if ($Expected) { Assert-ValidReferenceBody -Body $Anchor }
        else { Assert-NoReferenceBody -Body $Anchor }
    }

    It 'uses the first duplicate HTML href attribute: <Case>' -ForEach @(
        @{ Case = 'external before local'; Expected = $false; Anchor = '<a href="https://example.test" href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
        @{ Case = 'local before external'; Expected = $true; Anchor = '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741" href="https://example.test">tracking</a>' }
        @{ Case = 'empty before local'; Expected = $false; Anchor = '<a href="" href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
        @{ Case = 'bare before local'; Expected = $false; Anchor = '<a href href="https://github.com/Gibbs-Morris/mississippi/issues/741">context</a>' }
    ) {
        if ($Expected) { Assert-ValidReferenceBody -Body $Anchor }
        else { Assert-NoReferenceBody -Body $Anchor }
    }

    It 'preserves tracking between an inline code anchor opener and a later rendered anchor' {
        Assert-ValidReferenceBody -Body 'Example `<a href="https://github.com/other/repo/issues/999">` is code. Refs #741. <a href="https://github.com/other/repo/issues/999">context</a>'
    }

    It 'ignores an issue anchor opener inside inline code before a rendered external anchor' {
        Assert-NoReferenceBody -Body 'Example `<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">` is code. <a href="https://github.com/other/repo/issues/999">context</a>'
    }

    It 'preserves rendered tracking after an inline code span containing a longer backtick run' {
        Assert-ValidReferenceBody -Body 'Example `<a href="https://github.com/other/repo/issues/999">`` x` is code. Refs #741. <a href="https://github.com/other/repo/issues/999">context</a>'
    }

    It 'ignores a code anchor when its span contains a longer backtick run' {
        Assert-NoReferenceBody -Body 'Example `<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">`` x` is code. <a href="https://github.com/other/repo/issues/999">context</a>'
    }

    It 'preserves a reference after inline comment opener code' {
        Assert-ValidReferenceBody -Body 'The token `<!--` is code. Refs #741.'
    }

    It 'preserves tracking between raw HTML tags containing quoted backticks: <Case>' -ForEach @(
        @{ Case = 'span attributes'; Body = '<span title="`">left</span> Refs #741 <span title="`">right</span>' }
        @{ Case = 'anchor attributes'; Body = '<a href="https://github.com/other/repo/issues/999" title="`">left</a> Refs #741 <a href="https://github.com/other/repo/issues/999" title="`">right</a>' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores tracking inside code containing an HTML tag with a quoted backtick' {
        Assert-NoReferenceBody -Body 'Example ``<span title="`">Refs #741</span>`` is code.'
    }

    It 'ignores code delimited by backticks inside escaped HTML-like tags' {
        Assert-NoReferenceBody -Body '\<span title="`">Refs #741\<span title="`">'
    }

    It 'ignores code delimited by backticks inside escaped anchor-like tags' {
        Assert-NoReferenceBody -Body '\<a href="https://example.test" title="`">Refs #741\<a href="https://example.test" title="`">'
    }

    It 'preserves tracking between actual tags after an even number of backslashes' {
        Assert-ValidReferenceBody -Body '\\<span title="`">Refs #741\\<span title="`">'
    }

    It 'preserves tracking after code containing escaped HTML-like tags' {
        $outcome = Assert-ValidReferenceBody -Body '\<span title="`">Refs #742\<span title="`"> Refs #741'
        @($outcome.Result.References).Count | Should -Be 1
        $outcome.Result.References[0].Number | Should -Be 741
    }

    It 'ignores commented tracking between HTML tags containing quoted backticks' {
        Assert-NoReferenceBody -Body '<span title="`">left</span> <!-- Refs #741 --> <span title="`">right</span>'
    }

    It 'preserves a reference-like line that continues a paragraph: <Case>' -ForEach @(
        @{ Case = 'single-line destination'; Body = "Context`n[tracking]: https://github.com/Gibbs-Morris/mississippi/issues/741" }
        @{ Case = 'continuation destination'; Body = "Context`n[tracking]:`n  https://github.com/Gibbs-Morris/mississippi/issues/741" }
        @{ Case = 'escaped shortcut'; Body = '\[work]' + [Environment]::NewLine + '[work]: https://github.com/Gibbs-Morris/mississippi/issues/741' }
    ) {
        Assert-ValidReferenceBody -Body $Body
    }

    It 'ignores definitions after a paragraph boundary and inside code: <Case>' -ForEach @(
        @{ Case = 'blank-line boundary'; Body = "Context`n`n[unused]: https://github.com/Gibbs-Morris/mississippi/issues/741" }
        @{ Case = 'code'; Body = "    Context`n    [tracking]: https://github.com/Gibbs-Morris/mississippi/issues/741" }
    ) {
        Assert-NoReferenceBody -Body $Body
    }

    It 'ignores an unused Markdown reference definition' {
        Assert-NoReferenceBody -Body '[tracking]: https://github.com/Gibbs-Morris/mississippi/issues/741'
    }

    It 'ignores an unused multiline Markdown reference definition' {
        $body = "[unused]:`n  https://github.com/Gibbs-Morris/mississippi/issues/741"
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores a hidden continuation-line reference title' {
        $body = "[work]: https://example.test`n  (See https://github.com/Gibbs-Morris/mississippi/issues/741)"
        Assert-NoReferenceBody -Body $body
    }

    It 'allows a closed ancillary issue when an open issue is present' {
        Assert-ValidReferenceBody -Body 'Refs #741; supersedes #742.'
    }

    It 'returns structured output for an empty body' {
        $outcome = Invoke-ReferenceValidator -Body ''

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Valid | Should -BeFalse
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores fenced and HTML-comment examples' {
        $body = @'
```md
Refs #741
```
<!-- Refs #741 -->
'@
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'rejects missing references while ignoring cross-repository context' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #999; see https://github.com/other/repo/issues/741.'

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'does not exist'
    }

    It 'rejects external-only context without a local issue' {
        Assert-NoReferenceBody -Body 'Context: https://github.com/other/repo/issues/741.'
    }

    It 'ignores variable-length Markdown fences' {
        $body = @'
````md
Refs #741
`````
'@
        Assert-NoReferenceBody -Body $body
    }

    It 'does not close a fence indented beyond three spaces' {
        $backtick = [char]96
        $fence = [string]::new($backtick, 3)
        $body = $fence + 'md' + [Environment]::NewLine + 'Refs #741' + [Environment]::NewLine + '    ' + $fence
        Assert-NoReferenceBody -Body $body
        return
    }

    It 'does not treat backticks in fence info strings as a code fence opener' {
        $backtick = [char]96
        $fence = [string]::new($backtick, 3)
        $body = $fence + 'md' + $backtick + [Environment]::NewLine + 'Refs #741' + [Environment]::NewLine + $fence
        Assert-ValidReferenceBody -Body $body
    }

    It 'ignores variable-width inline code spans' {
        Assert-NoReferenceBody -Body '``Refs #741``'
    }

    It 'does not hide text when the inline-code closing run length differs' {
        Assert-ValidReferenceBody -Body '``Refs #741```'
    }

    It 'preserves references after escaped backticks' {
        $backtick = [char]96
        $body = '\' + $backtick + 'Refs #741' + $backtick
        Assert-ValidReferenceBody -Body $body
    }

    It 'ignores indented Markdown code blocks' {
        Assert-NoReferenceBody -Body "    Refs #741"
    }

    It 'ignores issue-looking tokens in raw HTML attributes' {
        Assert-NoReferenceBody -Body '<span title="#741">tracking</span>'
    }

    It 'ignores shorthand tokens in Markdown link destinations' {
        Assert-NoReferenceBody -Body '[tracking details](#741)'
    }

    It 'does not treat task-list markers as shortcut references' {
        $body = "Checklist:`n- [x] done`n`n[x]: https://github.com/Gibbs-Morris/mississippi/issues/741"
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores escaped shortcut labels' {
        $body = '\[work]' + [Environment]::NewLine + [Environment]::NewLine + '[work]: https://github.com/Gibbs-Morris/mississippi/issues/741'
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores issue URLs used as Markdown image destinations' {
        Assert-NoReferenceBody -Body '![tracking](https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores issue URLs in angle-enclosed Markdown image destinations' {
        Assert-NoReferenceBody -Body '![tracking](<https://github.com/Gibbs-Morris/mississippi/issues/741>)'
    }

    It 'preserves references after escaped closing brackets' {
        Assert-ValidReferenceBody -Body '\](Refs #741)'
    }

    It 'ignores shorthand tokens embedded in bare URLs' {
        Assert-NoReferenceBody -Body 'https://example.test/?issue=#741'
    }

    It 'caps API resolution work for excessive references' {
        $body = (1..25 | ForEach-Object { "Refs #$($_)" }) -join ' '
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'maximum supported is 20'
    }

    It 'preserves references after exhausting the Markdown link scan budget' {
        $body = ((1..100 | ForEach-Object { '[broken](' }) -join '') + ' Refs #741'
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'rejects closed issues' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #742'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Match 'not open'
    }

    It 'rejects a PR number masquerading as an issue' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #743'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Match 'pull request, not an issue'
    }

    It 'validates every constituent pull request in a merge-group fixture' {
        $fixture = @(
            [pscustomobject]@{ number = 101; body = 'Refs #741' },
            [pscustomobject]@{ number = 102; body = 'Refs Gibbs-Morris/mississippi#741' }
        )
        $outcome = Invoke-MergeGroupValidator -PullRequests $fixture

        $outcome.ExitCode | Should -Be 0
        (@($outcome.Output -split "`r?`n" | Select-String 'Validating merge-group pull request')).Count | Should -Be 2
    }

    It 'fails closed for an empty merge group' {
        $outcome = Invoke-MergeGroupValidator -PullRequests @()

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Output | Should -Match 'could not resolve any constituent[\s|]+pull requests'
    }

    It 'publishes PR-isolated status contexts' {
        $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/pr-issue-reference.yml') -Raw

        $workflow | Should -Match '\$statusContext = ''PR Issue Reference'''
        $workflow | Should -Match 'PR_NUMBER'
        $workflow | Should -Match 'DEFAULT_BRANCH'
        $workflow | Should -Match 'commits/\$env:DEFAULT_BRANCH'
        $workflow | Should -Not -Match 'actions/checkout'
    }

    It 'fails the merge group when one constituent PR has no open issue' {
        $fixture = @(
            [pscustomobject]@{ number = 101; body = 'Refs #741' },
            [pscustomobject]@{ number = 102; body = 'Refs #742' }
        )
        $outcome = Invoke-MergeGroupValidator -PullRequests $fixture

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Output | Should -Match 'failed for #102'
    }
}

#!/usr/bin/env pwsh

#requires -Module Pester
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Sonar container credential boundary' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/SonarContainerRuntime.psm1') -Force
        $mountRoot = [IO.Path]::GetFullPath($TestDrive)
        $parameters = @{Tools=(Join-Path $mountRoot 'tools');Driver=(Join-Path $mountRoot 'driver');Workspace=(Join-Path $mountRoot 'work');Cache=(Join-Path $mountRoot 'private');ScannerAssets=(Join-Path $mountRoot 'assets');BuildPackages=(Join-Path $mountRoot 'packages');AnalyzerMounts=@([pscustomobject]@{Source=(Join-Path $mountRoot 'assets/conf/SonarQubeAnalysisConfig.xml');Target='/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml'});Version='1.2.3'}
    }
    It 'isolates candidate <Phase> execution from credentials, private caches and writable tools' -TestCases @(@{Phase='Build'},@{Phase='Version'}) {
        param($Phase)
        $arguments = @(Get-SonarContainerArguments @parameters -Phase $Phase)
        $arguments | Should -Contain '--read-only'
        $arguments | Should -Contain '--user=1000:1000'
        $arguments | Should -Contain '--cap-drop=ALL'
        $arguments | Should -Contain '--security-opt=no-new-privileges'
        $arguments | Should -Contain '--env=HOME=/tmp/home'
        $arguments | Should -Contain '--env=XDG_CACHE_HOME=/tmp/home/.cache'
        ($arguments -join ' ') | Should -Not -Match 'SONAR_ANALYSIS_TOKEN|GH_TOKEN|GITHUB_TOKEN|docker.sock|target=/cache'
        $arguments | Should -Contain "type=bind,source=$($parameters.Tools),target=/tools,readonly"
        $arguments | Should -Contain "type=bind,source=$($parameters.Driver),target=/driver,readonly"
        $arguments | Should -Contain 'mcr.microsoft.com/dotnet/sdk@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317'
    }
    It 'uses read-only original scanner assets during the build' {
        $arguments = @(Get-SonarContainerArguments @parameters -Phase Build)
        $arguments | Should -Contain "type=bind,source=$($parameters.ScannerAssets)/bin,target=/work/.sonarqube/bin,readonly"
        $arguments | Should -Contain "type=bind,source=$($parameters.AnalyzerMounts[0].Source),target=/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml,readonly"
    }
    It 'passes only the named Sonar credential into trusted <Phase> execution' -TestCases @(@{Phase='Begin'},@{Phase='End'}) {
        param($Phase)
        $arguments = @(Get-SonarContainerArguments @parameters -Phase $Phase)
        $arguments | Should -Contain '--env=SONAR_ANALYSIS_TOKEN'
        $arguments | Should -Contain '--env=TMPDIR=/cache/tmp'
        $arguments | Should -Contain "type=bind,source=$($parameters.Cache),target=/cache"
        ($arguments -join ' ') | Should -Not -Match 'GH_TOKEN|GITHUB_TOKEN|SONAR_TOKEN='
    }
    It 'allows tool installation only during Prepare' {
        $arguments = @(Get-SonarContainerArguments @parameters -Phase Prepare)
        $arguments | Should -Contain "type=bind,source=$($parameters.Tools),target=/tools"
        ($arguments -join ' ') | Should -Not -Match 'SONAR_ANALYSIS_TOKEN|target=/cache'
    }
    It 'rejects ambiguous mount paths' -TestCases @(@{Value='relative/path'},@{Value='/tmp/comma,readonly'},@{Value="/tmp/new`nline"}) {
        param($Value)
        { Get-SonarContainerArguments -Tools $Value -Driver $parameters.Driver -Phase Prepare } | Should -Throw
    }
    It 'requires protected assets for an instrumented candidate build' {
        { Get-SonarContainerArguments -Tools $parameters.Tools -Driver $parameters.Driver -Workspace $parameters.Workspace -Phase Build -BuildPackages $parameters.BuildPackages } | Should -Throw '*scanner assets*'
    }
    It 'rejects actual credential bytes in exposed scanner files' {
        $assets = Join-Path $TestDrive 'credential-assets'; New-Item -ItemType Directory $assets -Force | Out-Null
        'safe configuration' | Set-Content (Join-Path $assets 'config.xml')
        { Assert-SonarAssetCredentialAbsent -Directory $assets -Token 'unique-token-value' } | Should -Not -Throw
        'hidden unique-token-value secret' | Set-Content (Join-Path $assets 'config.xml')
        { Assert-SonarAssetCredentialAbsent -Directory $assets -Token 'unique-token-value' } | Should -Throw '*protected credential*'
        { Assert-SonarAssetCredentialAbsent -Directory $assets -Token '' } | Should -Throw
    }
    It 'refuses to check out a foreign repository, malformed revision or existing workspace' {
        { Initialize-SonarSourceWorkspace -Path (Join-Path $TestDrive 'source') -Repository other/repo -Revision ('a'*40) -Branch main -DefaultBranch main -TargetRef main } | Should -Throw
        { Initialize-SonarSourceWorkspace -Path (Join-Path $TestDrive 'source') -Repository Gibbs-Morris/mississippi -Revision bad -Branch main -DefaultBranch main -TargetRef main } | Should -Throw
        { Initialize-SonarSourceWorkspace -Path $TestDrive -Repository Gibbs-Morris/mississippi -Revision ('a'*40) -Branch main -DefaultBranch main -TargetRef main } | Should -Throw '*fresh directory*'
    }
    It 'fails a container stage when Docker fails rather than uploading partial output' {
        Mock Invoke-SonarNative -ModuleName SonarContainerRuntime { throw 'Docker failed' }
        { Invoke-SonarContainer @parameters -Phase Build } | Should -Throw '*Docker failed*'
    }
    It 'fetches the exact revision into a fresh checkout with no credentials or hooks' {
        $script:nativeCalls = [Collections.Generic.List[object]]::new()
        Mock Invoke-SonarNative -ModuleName SonarContainerRuntime { $script:nativeCalls.Add([pscustomobject]@{Executable=$Executable;Arguments=@($Arguments)}) }
        $path = Join-Path $TestDrive 'immutable-checkout'
        Initialize-SonarSourceWorkspace -Path $path -Repository Gibbs-Morris/mississippi -Revision ('a'*40) -Branch pull/5/merge -DefaultBranch main -TargetRef main
        $script:nativeCalls.Count | Should -Be 5
        $script:nativeCalls[1].Arguments | Should -Contain '/dev/null'
        $script:nativeCalls[2].Arguments | Should -Contain 'https://github.com/Gibbs-Morris/mississippi.git'
        $script:nativeCalls[3].Arguments | Should -Contain ('a'*40)
        $script:nativeCalls[3].Arguments | Should -Contain '+refs/heads/main:refs/remotes/origin/main'
        $script:nativeCalls[4].Arguments | Should -Contain 'pull/5/merge'
        ($script:nativeCalls.Arguments -join ' ') | Should -Not -Match 'token|extraheader|credential'
    }

    It 'fetches the actual <Target> target for both real controller workspace calls' -TestCases @(
        @{Target='feature/example'},@{Target='topic/example'},@{Target='main'}
    ) {
        param($Target)
        $tokens=$null;$errors=$null
        $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repoRoot 'eng/src/agent-scripts/invoke-trusted-sonar-analysis.ps1'),[ref]$tokens,[ref]$errors)
        $errors.Count | Should -Be 0
        $calls=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -ceq 'Initialize-SonarSourceWorkspace'},$true))
        $calls.Count | Should -Be 2
        $setup=[scriptblock]::Create((@($calls | ForEach-Object {$_.Extent.Text}) -join [Environment]::NewLine))
        $script:nativeCalls=[Collections.Generic.List[object]]::new()
        Mock Invoke-SonarNative -ModuleName SonarContainerRuntime {$script:nativeCalls.Add([pscustomobject]@{Executable=$Executable;Arguments=@($Arguments)})}
        $Repository='Gibbs-Morris/mississippi';$DefaultBranch='main';$branch='pull/5/merge'
        $source=[pscustomobject]@{BuildSha=('a'*40);TargetRef=$Target}
        $build=Join-Path $TestDrive 'controller-build';$upload=Join-Path $TestDrive 'controller-upload'
        & $setup
        $script:nativeCalls.Count | Should -Be 10
        $fetches=@($script:nativeCalls | Where-Object {$_.Arguments -contains 'fetch'})
        $fetches.Count | Should -Be 2
        foreach($fetch in $fetches){
            $fetch.Arguments | Should -Contain "+refs/heads/${Target}:refs/remotes/origin/$Target"
            $fetch.Arguments | Should -Contain $source.BuildSha
            $fetch.Arguments | Should -Contain '--tags'
            $fetch.Arguments | Should -Contain '+refs/heads/main:refs/remotes/origin/main'
            @($fetch.Arguments | Where-Object {$_ -like '+refs/heads/*'}).Count | Should -Be $(if($Target -ceq 'main'){1}else{2})
        }
        ($script:nativeCalls.Arguments -join ' ') | Should -Not -Match 'token|extraheader|credential'
    }

    It 'retains real release tags in both immutable source workspaces' {
        $script:realSonarNative = & (Get-Module SonarContainerRuntime) { (Get-Command Invoke-SonarNative).ScriptBlock }
        $script:versionRemote = Join-Path $TestDrive 'version-remote'
        & $script:realSonarNative git @('init','--initial-branch=main',$script:versionRemote)
        'release' | Set-Content (Join-Path $script:versionRemote 'file.txt')
        & $script:realSonarNative git @('-C',$script:versionRemote,'add','file.txt')
        & $script:realSonarNative git @('-C',$script:versionRemote,'-c','user.name=Fixture','-c','user.email=fixture@example.com','commit','-m','release')
        $release = (& git -C $script:versionRemote rev-parse HEAD).Trim()
        $LASTEXITCODE | Should -Be 0
        & $script:realSonarNative git @('-C',$script:versionRemote,'tag','v1.2.3')
        & $script:realSonarNative git @('-C',$script:versionRemote,'-c','user.name=Fixture','-c','user.email=fixture@example.com','tag','-a','v1.2.4','-m','annotated release')
        'candidate' | Set-Content (Join-Path $script:versionRemote 'file.txt')
        & $script:realSonarNative git @('-C',$script:versionRemote,'add','file.txt')
        & $script:realSonarNative git @('-C',$script:versionRemote,'-c','user.name=Fixture','-c','user.email=fixture@example.com','commit','-m','candidate')
        $revision = (& git -C $script:versionRemote rev-parse HEAD).Trim()
        $LASTEXITCODE | Should -Be 0
        Mock Invoke-SonarNative -ModuleName SonarContainerRuntime {
            $Executable | Should -Be 'git'
            $localArguments = @($Arguments | ForEach-Object {
                if ($_ -ceq 'https://github.com/Gibbs-Morris/mississippi.git') { $script:versionRemote } else { $_ }
            })
            & $script:realSonarNative -Executable $Executable -Arguments $localArguments
        }
        foreach ($workspace in @('version-build','version-upload')) {
            $path = Join-Path $TestDrive $workspace
            Initialize-SonarSourceWorkspace -Path $path -Repository Gibbs-Morris/mississippi -Revision $revision -Branch pull/5/merge -DefaultBranch main -TargetRef main
            (& git -C $path rev-parse HEAD).Trim() | Should -BeExactly $revision
            $LASTEXITCODE | Should -Be 0
            @(& git -C $path tag --list) | Should -Contain 'v1.2.3'
            $LASTEXITCODE | Should -Be 0
            @(& git -C $path tag --list) | Should -Contain 'v1.2.4'
            $LASTEXITCODE | Should -Be 0
            (& git -C $path rev-parse 'v1.2.3^{commit}').Trim() | Should -BeExactly $release
            $LASTEXITCODE | Should -Be 0
            (& git -C $path rev-parse 'v1.2.4^{commit}').Trim() | Should -BeExactly $release
            $LASTEXITCODE | Should -Be 0
            { Assert-SonarSourceWorkspaceClean -Path $path -Revision $revision } | Should -Not -Throw
        }
    }

    It 'matches the runner ownership while requiring a non-root container identity' {
        $arguments = @(Get-SonarContainerArguments @parameters -Phase Build -UserId 1001 -GroupId 118)
        $arguments | Should -Contain '--user=1001:118'
        { Get-SonarContainerArguments @parameters -Phase Build -UserId 0 } | Should -Throw
        { Get-SonarContainerArguments @parameters -Phase Build -GroupId 0 } | Should -Throw
    }
    It 'projects only requested analyzer DLLs and immutable global configuration' {
        $scanner=Join-Path $TestDrive 'scanner';$cache=Join-Path $TestDrive 'private-cache';$projection=Join-Path $TestDrive 'projection';$build=Join-Path $TestDrive 'projection-build'
        foreach($directory in @((Join-Path $scanner 'conf/cs'),(Join-Path $cache 'resources/0'),$projection,$build)){New-Item -ItemType Directory $directory -Force | Out-Null}
        '<AnalysisConfig><AnalyzersSettings><AnalyzerSettings><AnalyzerPlugins><AnalyzerPlugin Key="csharp"><AssemblyPaths><Path>/cache/resources/0/analyzer.dll</Path></AssemblyPaths></AnalyzerPlugin></AnalyzerPlugins><AdditionalFilePaths><Path>/work/.sonarqube/conf/cs/SonarLint.xml</Path></AdditionalFilePaths></AnalyzerSettings></AnalyzersSettings></AnalysisConfig>' | Set-Content (Join-Path $scanner 'conf/SonarQubeAnalysisConfig.xml')
        '<configuration />' | Set-Content (Join-Path $scanner 'conf/cs/SonarLint.xml')
        'assembly bytes' | Set-Content (Join-Path $cache 'resources/0/analyzer.dll')
        'private executable' | Set-Content (Join-Path $cache 'engine.jar')
        $mounts=@(Copy-SonarAnalyzerProjection -Scanner $scanner -PrivateCache $cache -Projection $projection -BuildRoot $build -Token 'fixture-credential')
        $mounts.Count | Should -Be 3
        $mounts.Target | Should -Contain '/cache/resources/0/analyzer.dll'
        $mounts.Target | Should -Contain '/work/.sonarqube/conf/cs/SonarLint.xml'
        Test-Path (Join-Path $projection 'engine.jar') | Should -BeFalse
        [IO.File]::ReadAllText((Join-Path $build '.sonarqube/conf/cs/SonarLint.xml')) | Should -Be ''
        $arguments=@(Get-SonarContainerArguments -Tools $parameters.Tools -Driver $parameters.Driver -Workspace $build -ScannerAssets $scanner -BuildPackages $parameters.BuildPackages -Phase Build -AnalyzerMounts $mounts)
        $arguments | Should -Contain "type=bind,source=$(Join-Path $projection 'resources/0/analyzer.dll'),target=/cache/resources/0/analyzer.dll,readonly"
        'fixture-credential' | Set-Content (Join-Path $cache 'resources/0/analyzer.dll')
        { Copy-SonarAnalyzerProjection -Scanner $scanner -PrivateCache $cache -Projection $projection -BuildRoot $build -Token 'fixture-credential' } | Should -Throw '*protected credential*'
    }
    It 'rejects unsafe analyzer mount destinations' {
        {Get-SonarContainerArguments -Tools $parameters.Tools -Driver $parameters.Driver -Workspace $parameters.Workspace -ScannerAssets $parameters.ScannerAssets -BuildPackages $parameters.BuildPackages -Phase Build -AnalyzerMounts @($parameters.AnalyzerMounts[0],[pscustomobject]@{Source=$parameters.Tools;Target='/cache/../private'})} | Should -Throw '*analyzer mount*'
    }
    It 'propagates native command failures' {
        InModuleScope SonarContainerRuntime {
            { Invoke-SonarNative -Executable pwsh -Arguments @('-NoProfile','-Command','exit 7') } | Should -Throw '*command failed*'
        }
    }
    It 'checks that source has not changed before upload' {
        $source = Join-Path $TestDrive 'clean-source'; New-Item -ItemType Directory $source | Out-Null
        & git init $source | Out-Null
        'unchanged' | Set-Content (Join-Path $source 'file.txt')
        & git -C $source add file.txt
        & git -C $source -c user.name=Fixture -c user.email=fixture@example.com commit -m fixture | Out-Null
        $revision = (& git -C $source rev-parse HEAD).Trim()
        { Assert-SonarSourceWorkspaceClean -Path $source -Revision $revision } | Should -Not -Throw
        { Assert-SonarSourceWorkspaceClean -Path $source -Revision ('a'*40) } | Should -Throw '*revision changed*'
        'modified' | Set-Content (Join-Path $source 'file.txt')
        { Assert-SonarSourceWorkspaceClean -Path $source -Revision $revision } | Should -Throw '*unchanged tracked source*'
    }
}

Describe 'Sonar environment admission' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
    }
    BeforeEach {
        $script:environment = [pscustomobject]@{deployment_branch_policy=[pscustomobject]@{protected_branches=$false;custom_branch_policies=$true}}
        $script:policy = [pscustomobject]@{total_count=1;branch_policies=@([pscustomobject]@{id=43382702;name='main';type='branch'})}
        $script:policyDetail = [pscustomobject]@{id=43382702;name='main';type='branch'}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if ($Path -like '*/deployment-branch-policies/43382702') { return $script:policyDetail }
            if ($Path -like '*deployment-branch-policies*') { return $script:policy }
            return $script:environment
        }
    }
    It 'accepts exactly one branch policy for main' {
        { Assert-SonarCredentialDeployment -Repository Gibbs-Morris/mississippi -DefaultBranch main } | Should -Not -Throw
    }
    It 'reads the exact policy detail when the list omits its type: <Shape>' -TestCases @(@{Shape='absent'},@{Shape='null'}) {
        param($Shape)
        if($Shape -eq 'absent'){$script:policy.branch_policies[0].PSObject.Properties.Remove('type')}else{$script:policy.branch_policies[0].type=$null}
        { Assert-SonarCredentialDeployment -Repository Gibbs-Morris/mississippi -DefaultBranch main } | Should -Not -Throw
        Should -Invoke Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis -Times 1 -Exactly -ParameterFilter {$Path -like '*/deployment-branch-policies/43382702'}
    }
    It 'rejects missing or inconsistent branch-type evidence: <Case>' -TestCases @(
        @{Case='missing ID'},@{Case='invalid ID'},@{Case='tag detail'},@{Case='wrong detail ID'},@{Case='wrong detail name'},@{Case='missing detail type'},@{Case='unavailable detail'}
    ) {
        param($Case)
        $script:policy.branch_policies[0].PSObject.Properties.Remove('type')
        switch($Case){
            'missing ID' {$script:policy.branch_policies[0].PSObject.Properties.Remove('id')}
            'invalid ID' {$script:policy.branch_policies[0].id='../other'}
            'tag detail' {$script:policyDetail.type='tag'}
            'wrong detail ID' {$script:policyDetail.id=99}
            'wrong detail name' {$script:policyDetail.name='other'}
            'missing detail type' {$script:policyDetail.PSObject.Properties.Remove('type')}
            'unavailable detail' {Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {throw 'Policy detail unavailable.'} -ParameterFilter {$Path -like '*/deployment-branch-policies/43382702'}}
        }
        { Assert-SonarCredentialDeployment -Repository Gibbs-Morris/mississippi -DefaultBranch main } | Should -Throw
    }
    It 'rejects unsafe environment configuration: <Case>' -TestCases @(@{Case='unprotected'},@{Case='all protected branches'},@{Case='wildcard'},@{Case='tag'},@{Case='extra policy'},@{Case='incomplete pagination'}) {
        param($Case)
        switch ($Case) {
            'unprotected' { $script:environment.deployment_branch_policy=$null }
            'all protected branches' { $script:environment.deployment_branch_policy.protected_branches=$true }
            'wildcard' { $script:policy.branch_policies[0].name='*' }
            'tag' { $script:policy.branch_policies[0].type='tag' }
            'extra policy' { $script:policy.total_count=2; $script:policy.branch_policies+=@([pscustomobject]@{name='other';type='branch'}) }
            'incomplete pagination' { $script:policy.total_count=200 }
        }
        { Assert-SonarCredentialDeployment -Repository Gibbs-Morris/mississippi -DefaultBranch main } | Should -Throw
    }
}

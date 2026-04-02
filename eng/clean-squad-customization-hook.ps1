[CmdletBinding()]
param()

$rawInput = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($rawInput)) {
    '{"continue":true}'
    exit 0
}

try {
    $payload = $rawInput | ConvertFrom-Json -Depth 20
} catch {
    '{"continue":true}'
    exit 0
}

function Find-CustomizationPath {
    param([object]$Value)

    if ($null -eq $Value) {
        return $false
    }

    if ($Value -is [string]) {
        return $Value -match '(^|[\\/])\.github[\\/]+agents[\\/]+cs-.*\.agent\.md$' -or
            $Value -match '(^|[\\/])\.github[\\/]+prompts[\\/]+.*\.prompt\.md$' -or
            $Value -match '(^|[\\/])\.github[\\/]+skills[\\/]+.*SKILL\.md$' -or
            $Value -match '(^|[\\/])\.github[\\/]+clean-squad[\\/]+WORKFLOW\.md$' -or
            $Value -match '(^|[\\/])\.github[\\/]+instructions[\\/]+clean-squad\.instructions\.md$' -or
            $Value -match '(^|[\\/])\.cursor[\\/]+rules[\\/]+clean-squad\.mdc$'
    }

    if ($Value -is [System.Collections.IDictionary]) {
        foreach ($entry in $Value.Values) {
            if (Find-CustomizationPath $entry) {
                return $true
            }
        }

        return $false
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        foreach ($item in $Value) {
            if (Find-CustomizationPath $item) {
                return $true
            }
        }

        return $false
    }

    foreach ($property in $Value.PSObject.Properties) {
        if (Find-CustomizationPath $property.Value) {
            return $true
        }
    }

    return $false
}

$isCustomizationEdit = $payload.hookEventName -eq 'PostToolUse' -and (Find-CustomizationPath $payload.tool_input)

if ($isCustomizationEdit) {
    @{
        continue = $true
        systemMessage = 'Clean Squad customization files changed. Run /clean-squad-validate-contracts or pwsh -File ./eng/validate-clean-squad-customizations.ps1 before considering the change complete.'
    } | ConvertTo-Json -Compress

    exit 0
}

'{"continue":true}'

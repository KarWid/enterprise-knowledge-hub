<#
.SYNOPSIS
Grants the storage-event webhook app role to Event Grid and the GitHub deployment identity.

.DESCRIPTION
Performs the one-time Microsoft Entra bootstrap required for authenticated Event Grid
webhook delivery. The script accepts application/client IDs as parameters, resolves the
corresponding directory object IDs at runtime, and creates only missing app-role assignments.

The script does not accept, persist, or print credentials, access tokens, connection strings,
or client secrets. Run it as an authorized administrator in the workforce Entra tenant.

.PARAMETER TenantId
The workforce Microsoft Entra tenant ID.

.PARAMETER WebhookApplicationId
The application/client ID of the EKH Storage Events app registration.

.PARAMETER EventGridApplicationId
The application/client ID of the Microsoft.EventGrid enterprise application in the tenant.

.PARAMETER GitHubDeploymentApplicationId
The application/client ID configured as AZURE_CLIENT_ID for GitHub Actions.

.EXAMPLE
.\infra\azure\bootstrap\Grant-StorageEventWebhookRoles.ps1 `
    -TenantId '<workforce-tenant-id>' `
    -WebhookApplicationId '<webhook-application-client-id>' `
    -EventGridApplicationId '<event-grid-application-client-id>' `
    -GitHubDeploymentApplicationId '<github-deployment-application-client-id>'

.EXAMPLE
.\infra\azure\bootstrap\Grant-StorageEventWebhookRoles.ps1 `
    -TenantId '<workforce-tenant-id>' `
    -WebhookApplicationId '<webhook-application-client-id>' `
    -EventGridApplicationId '<event-grid-application-client-id>' `
    -GitHubDeploymentApplicationId '<github-deployment-application-client-id>' `
    -WhatIf
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ $_ -ne [Guid]::Empty })]
    [Guid] $TenantId,

    [Parameter(Mandatory)]
    [ValidateScript({ $_ -ne [Guid]::Empty })]
    [Guid] $WebhookApplicationId,

    [Parameter(Mandatory)]
    [ValidateScript({ $_ -ne [Guid]::Empty })]
    [Guid] $EventGridApplicationId,

    [Parameter(Mandatory)]
    [ValidateScript({ $_ -ne [Guid]::Empty })]
    [Guid] $GitHubDeploymentApplicationId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$roleValue = 'AzureEventGridSecureWebhookSubscriber'
$graphBaseUrl = 'https://graph.microsoft.com/v1.0'

function Assert-DistinctApplicationIds
{
    $applicationIds = @(
        $WebhookApplicationId
        $EventGridApplicationId
        $GitHubDeploymentApplicationId
    )

    $distinctApplicationIds = @(
        $applicationIds |
            Select-Object -Unique
    )

    if ($distinctApplicationIds.Count -ne $applicationIds.Count)
    {
        throw 'Webhook, Microsoft.EventGrid, and GitHub deployment application IDs must be different.'
    }
}

function Invoke-AzCliJson
{
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [Parameter(Mandatory)]
        [string] $FailureMessage
    )

    $output = & az @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw $FailureMessage
    }

    $json = $output -join [Environment]::NewLine

    if ([string]::IsNullOrWhiteSpace($json))
    {
        throw "$FailureMessage Azure CLI returned an empty response."
    }

    try
    {
        return $json | ConvertFrom-Json
    }
    catch
    {
        throw "$FailureMessage Azure CLI returned invalid JSON."
    }
}

function Get-Application
{
    param(
        [Parameter(Mandatory)]
        [Guid] $ApplicationId,

        [Parameter(Mandatory)]
        [string] $ApplicationName
    )

    return Invoke-AzCliJson `
        -Arguments @(
            'ad'
            'app'
            'show'
            '--id'
            $ApplicationId.ToString()
            '--output'
            'json'
            '--only-show-errors'
        ) `
        -FailureMessage "$ApplicationName application was not found in tenant $TenantId."
}

function Get-ServicePrincipal
{
    param(
        [Parameter(Mandatory)]
        [Guid] $ApplicationId,

        [Parameter(Mandatory)]
        [string] $PrincipalName
    )

    return Invoke-AzCliJson `
        -Arguments @(
            'ad'
            'sp'
            'show'
            '--id'
            $ApplicationId.ToString()
            '--output'
            'json'
            '--only-show-errors'
        ) `
        -FailureMessage "$PrincipalName service principal was not found in tenant $TenantId."
}

function Assert-ApplicationId
{
    param(
        [Parameter(Mandatory)]
        $DirectoryObject,

        [Parameter(Mandatory)]
        [Guid] $ExpectedApplicationId,

        [Parameter(Mandatory)]
        [string] $ObjectName
    )

    try
    {
        $actualApplicationId = [Guid] $DirectoryObject.appId
    }
    catch
    {
        throw "$ObjectName returned an invalid application ID."
    }

    if ($actualApplicationId -ne $ExpectedApplicationId)
    {
        throw "$ObjectName does not match the supplied application ID."
    }
}

function Grant-WebhookAppRole
{
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory)]
        $Principal,

        [Parameter(Mandatory)]
        [string] $PrincipalName,

        [Parameter(Mandatory)]
        $WebhookServicePrincipal,

        [Parameter(Mandatory)]
        $AppRole
    )

    $assignmentsUrl =
        "$graphBaseUrl/servicePrincipals/$($Principal.id)/appRoleAssignments?`$select=id,resourceId,appRoleId&`$top=999"

    $assignmentsResponse = Invoke-AzCliJson `
        -Arguments @(
            'rest'
            '--method'
            'GET'
            '--url'
            $assignmentsUrl
            '--output'
            'json'
            '--only-show-errors'
        ) `
        -FailureMessage "Could not read app-role assignments for $PrincipalName."

    $existingAssignment = $assignmentsResponse.value |
        Where-Object {
            $_.resourceId -eq $WebhookServicePrincipal.id -and
            $_.appRoleId -eq $AppRole.id
        } |
        Select-Object -First 1

    if ($null -ne $existingAssignment)
    {
        Write-Host "$PrincipalName already has the $roleValue role."
        return
    }

    if (-not $PSCmdlet.ShouldProcess(
        $PrincipalName,
        "Assign $roleValue on the EKH Storage Events application"))
    {
        return
    }

    $requestBody = @{
        principalId = $Principal.id
        resourceId = $WebhookServicePrincipal.id
        appRoleId = $AppRole.id
    } |
        ConvertTo-Json -Compress

    # A short-lived file avoids PowerShell/Azure CLI JSON quoting problems. It contains
    # directory object IDs only, is created in the OS temp directory, and is always removed.
    $requestFile = Join-Path `
        ([System.IO.Path]::GetTempPath()) `
        "ekh-app-role-$([Guid]::NewGuid().ToString('N')).json"

    try
    {
        [System.IO.File]::WriteAllText($requestFile, $requestBody)

        $createAssignmentUrl =
            "$graphBaseUrl/servicePrincipals/$($Principal.id)/appRoleAssignments"

        & az rest `
            --method POST `
            --url $createAssignmentUrl `
            --body "@$requestFile" `
            --output none `
            --only-show-errors

        if ($LASTEXITCODE -ne 0)
        {
            throw "Could not assign $roleValue to $PrincipalName."
        }

        Write-Host "Assigned $roleValue to $PrincipalName."
    }
    finally
    {
        if (Test-Path -LiteralPath $requestFile)
        {
            Remove-Item -LiteralPath $requestFile -Force
        }
    }
}

Assert-DistinctApplicationIds

if ($null -eq (Get-Command az -ErrorAction SilentlyContinue))
{
    throw 'Azure CLI is not installed or is not available on PATH.'
}

Write-Host 'Signing in to the workforce Entra tenant...'

& az login `
    --tenant $TenantId.ToString() `
    --allow-no-subscriptions `
    --only-show-errors `
    --output none

if ($LASTEXITCODE -ne 0)
{
    throw 'Azure CLI login failed.'
}

$currentTenantId = & az account show `
    --query tenantId `
    --output tsv `
    --only-show-errors

if ($LASTEXITCODE -ne 0)
{
    throw 'Could not read the current Azure CLI account.'
}

$currentTenantId = $currentTenantId.Trim()

if ($currentTenantId -ne $TenantId.ToString())
{
    throw "Azure CLI is connected to an unexpected tenant. Sign in to tenant $TenantId and try again."
}

Write-Host 'Resolving the EKH Storage Events application...'

$webhookApplication = Get-Application `
    -ApplicationId $WebhookApplicationId `
    -ApplicationName 'EKH Storage Events'

Assert-ApplicationId `
    -DirectoryObject $webhookApplication `
    -ExpectedApplicationId $WebhookApplicationId `
    -ObjectName 'EKH Storage Events application'

$webhookServicePrincipal = Get-ServicePrincipal `
    -ApplicationId $WebhookApplicationId `
    -PrincipalName 'EKH Storage Events'

Assert-ApplicationId `
    -DirectoryObject $webhookServicePrincipal `
    -ExpectedApplicationId $WebhookApplicationId `
    -ObjectName 'EKH Storage Events service principal'

Write-Host 'Resolving the Microsoft.EventGrid service principal...'

$eventGridServicePrincipal = Get-ServicePrincipal `
    -ApplicationId $EventGridApplicationId `
    -PrincipalName 'Microsoft.EventGrid'

Assert-ApplicationId `
    -DirectoryObject $eventGridServicePrincipal `
    -ExpectedApplicationId $EventGridApplicationId `
    -ObjectName 'Microsoft.EventGrid service principal'

if ([string] $eventGridServicePrincipal.displayName -cne 'Microsoft.EventGrid')
{
    throw 'The Event Grid service principal display name must be Microsoft.EventGrid.'
}

Write-Host 'Resolving the GitHub deployment service principal...'

$githubServicePrincipal = Get-ServicePrincipal `
    -ApplicationId $GitHubDeploymentApplicationId `
    -PrincipalName 'GitHub deployment application'

Assert-ApplicationId `
    -DirectoryObject $githubServicePrincipal `
    -ExpectedApplicationId $GitHubDeploymentApplicationId `
    -ObjectName 'GitHub deployment service principal'

$matchingRoles = @(
    $webhookServicePrincipal.appRoles |
        Where-Object {
            $_.value -eq $roleValue -and
            $_.isEnabled
        }
)

if ($matchingRoles.Count -ne 1)
{
    throw "Expected exactly one enabled $roleValue app role, but found $($matchingRoles.Count)."
}

$appRole = $matchingRoles[0]

if (@($appRole.allowedMemberTypes) -notcontains 'Application')
{
    throw "The $roleValue role must allow Application members."
}

Write-Host "Webhook application: $($webhookApplication.displayName)"
Write-Host "Microsoft.EventGrid principal: $($eventGridServicePrincipal.displayName)"
Write-Host "GitHub deployment principal: $($githubServicePrincipal.displayName)"

Grant-WebhookAppRole `
    -Principal $eventGridServicePrincipal `
    -PrincipalName 'Microsoft.EventGrid' `
    -WebhookServicePrincipal $webhookServicePrincipal `
    -AppRole $appRole

Grant-WebhookAppRole `
    -Principal $githubServicePrincipal `
    -PrincipalName 'GitHub deployment application' `
    -WebhookServicePrincipal $webhookServicePrincipal `
    -AppRole $appRole

Write-Host 'Role-assignment bootstrap completed.'

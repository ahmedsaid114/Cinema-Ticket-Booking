$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $PSScriptRoot 'TaskCinema.csproj'
if (-not (Test-Path -LiteralPath $projectFile)) {
    throw 'TaskCinema.csproj was not found next to this script.'
}

function Convert-SecureStringToPlainText([Security.SecureString] $value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Set-ProjectSecret([string] $name, [string] $value) {
    & dotnet user-secrets set $name $value --project $projectFile
    if ($LASTEXITCODE -ne 0) {
        throw "Could not save $name. Make sure the .NET SDK is installed."
    }
}

$superAdminEmail = (Read-Host 'SuperAdmin email').Trim()
$superAdminPassword = Convert-SecureStringToPlainText (Read-Host 'SuperAdmin password (8+ chars, upper/lowercase and a digit)' -AsSecureString)
$adminEmail = (Read-Host 'Admin email').Trim()
$adminPassword = Convert-SecureStringToPlainText (Read-Host 'Admin password (8+ chars, upper/lowercase and a digit)' -AsSecureString)

if ([string]::IsNullOrWhiteSpace($superAdminEmail) -or [string]::IsNullOrWhiteSpace($adminEmail)) {
    throw 'Both email addresses are required.'
}
if ($superAdminEmail -ieq $adminEmail) {
    throw 'Use different email addresses for SuperAdmin and Admin.'
}
foreach ($entry in @(@('SuperAdmin', $superAdminPassword), @('Admin', $adminPassword))) {
    $name = $entry[0]
    $password = $entry[1]
    if ($password.Length -lt 8 -or $password -cnotmatch '[A-Z]' -or $password -cnotmatch '[a-z]' -or $password -notmatch '\d') {
        throw "$name password does not meet the app requirements. Use at least 8 characters with uppercase, lowercase, and a digit."
    }
}

Set-ProjectSecret 'SuperAdmin:Email' $superAdminEmail
Set-ProjectSecret 'SuperAdmin:Password' $superAdminPassword
Set-ProjectSecret 'Admin:Email' $adminEmail
Set-ProjectSecret 'Admin:Password' $adminPassword

Write-Host ''
Write-Host 'Admin and SuperAdmin credentials saved to local .NET User Secrets.' -ForegroundColor Green
Write-Host 'Now start the app with the Development profile. It will apply migrations and create the roles and accounts.'
Write-Host 'SuperAdmin sign-in: /Admin/Account/Login    SuperAdmin dashboard: /Admin/SuperAdmin'
Write-Host 'Admin sign-in: /Admin/Account/Login        Admin dashboard: /Admin/Home'

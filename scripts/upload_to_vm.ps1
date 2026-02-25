# Nicodemouse VM Upload Helper
# This script helps upload files to the Oracle Cloud VM

$IP = "144.22.254.132"
$USER = "ubuntu"
$KEY = "C:\Users\777\Documents\.conti\ssh-key-2026-01-26.key"
$REMOTE_PATH = "~/app/"

function Send-ItemToVM {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [string]$TargetSuffix = ""
    )

    $FullTargetPath = "$REMOTE_PATH$TargetSuffix"
    Write-Output "[UPLOAD] Uploading $Path to $USER@$IP`:$FullTargetPath"
    
    if (Test-Path -Path $Path -PathType Container) {
        # Directory upload
        scp -i $KEY -r "$Path/*" "$USER@$IP`:$FullTargetPath"
    }
    else {
        # File upload
        scp -i $KEY $Path "$USER@$IP`:$FullTargetPath"
    }
}

# Example Usages:
# 1. Upload Signaling Server:
# ./upload_to_vm.ps1 -Path ./server -TargetSuffix "server"
#
# 2. Upload Built Distribution:
# ./upload_to_vm.ps1 -Path ./releases/nicodemouse-release.zip

if ($args.Count -gt 0) {
    $target = if ($args.Count -ge 2) { $args[1] } else { "" }
    Send-ItemToVM -Path $args[0] -TargetSuffix $target
}
else {
    Write-Output "Usage: ./upload_to_vm.ps1 <LocalPath> [TargetSuffix]"
    Write-Output "Example: ./upload_to_vm.ps1 ./server server"
}

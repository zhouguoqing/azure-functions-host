$buildReason = $env:BUILD_REASON
$sourceBranch = $env:BUILD_SOURCEBRANCH
$bypassPackaging = 1
$includeSuffix = 1
Write-Host "SourceBranch: $sourceBranch, Build reason: $buildReason"

if($sourceBranch.endsWith('devops_experiment') -and ($buildReason -ne "PullRequest"))
{
  $includeSuffix = 0 #Setting to false
  $bypassPackaging = 0
}
elseif($buildReason -eq "PullRequest")
{
  $response = Invoke-RestMethod api.github.com/repos/$env:BUILD_REPOSITORY_ID/pulls/$env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER
  $title = $response.title.ToLowerInvariant()
  if ($title.Contains("[pack]")) {
    $bypassPackaging = 0
  }
}

# Write to output
"##vso[task.setvariable variable=IncludeSuffix;isOutput=true]$includeSuffix"
"##vso[task.setvariable variable=BypassPackaging;isOutput=true]$bypassPackaging"

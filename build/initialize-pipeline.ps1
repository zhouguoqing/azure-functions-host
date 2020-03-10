$buildReason = $env:BUILD_REASON
Write-Host "Build reason: $buildReason"

if ($buildReason -eq "PullRequest") {
  # parse PR title to see if we should pack this
  $response = Invoke-RestMethod api.github.com/repos/$env:BUILD_REPOSITORY_ID/pulls/$env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER
  $env:PULLREQUEST_TITLE = $response.title.ToLowerInvariant()
}

$buildReason = $env:BUILD_REASON
$title = "N/A"
Write-Host "Build reason: $buildReason"
"##vso[task.setvariable variable=PullRequestTitle]$title"

if ($buildReason -eq "PullRequest") {
  # parse PR title to see if we should pack this
  $response = Invoke-RestMethod api.github.com/repos/$env:BUILD_REPOSITORY_ID/pulls/$env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER
  $title = $response.title.ToLowerInvariant()
  "##vso[task.setvariable variable=PullRequestTitle]$title"
  Write-Host "Title: $title"
}

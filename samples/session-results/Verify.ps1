$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'SessionResults.cs')
if ([Portfolio.SessionResults.StartLevel]::Resolve(6, [int[]]@(8, 2, 4)) -ne 4) {
    throw 'An unsupported level should resolve downward.'
}
if ([Portfolio.SessionResults.StartLevel]::Resolve(0, [int[]]@(8, 2, 4)) -ne 2) {
    throw 'A request below the range should use the lowest level.'
}
$trialType = [Portfolio.SessionResults.Trial[string]]
$outcome = [Portfolio.SessionResults.Outcome]
$serializer = [Portfolio.SessionResults.ResultJson]
$correct = $trialType::new(1, 100, 350, $outcome::Correct, 'selected blue')
$timeout = $trialType::new(2, 1600, $null, $outcome::TimedOut, 'no response')
$roundType = [Portfolio.SessionResults.Round[string]]
$round = $roundType::new(1, 4, [Portfolio.SessionResults.Trial[string][]]@($correct, $timeout))
$json = $serializer::Serialize[string]($round)
$result = $json | ConvertFrom-Json
if ($result.level -ne 4 -or $result.trials.Count -ne 2 -or
    $result.trials[0].firstResponseMs -ne 350 -or
    $result.trials[1].outcome -ne 'timedOut' -or
    $null -ne $result.trials[1].firstResponseMs) {
    throw 'A valid round must preserve response times and the timeout.'
}
function Assert-Rejected($trials) {
    $invalidRound = $roundType::new(1, 4, $trials)
    try { $null = $serializer::Serialize[string]($invalidRound) }
    catch [System.ArgumentException] { return }
    throw 'Invalid trials were accepted.'
}
Assert-Rejected ([Portfolio.SessionResults.Trial[string][]]@($correct, $correct))
$invalidTime = $trialType::new(2, 1600, -1, $outcome::Incorrect, 'selected red')
Assert-Rejected ([Portfolio.SessionResults.Trial[string][]]@($correct, $invalidTime))
'PASS: level fallback, valid payload, timeout, duplicate trial, and negative time'
$json

param(
    [string]$Model = 'qwen2.5:3b',
    [string]$BaseUrl = 'http://127.0.0.1:11434'
)
$ErrorActionPreference = 'Stop'
$resources = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets/Resources'
$prompt = Get-Content (Join-Path $resources 'WatchmanPrompt.txt') -Encoding UTF8 -Raw
$schema = Get-Content (Join-Path $resources 'DialogueSchema.json') -Encoding UTF8 -Raw | ConvertFrom-Json
$body = @{
    model = $Model
    stream = $false
    format = $schema
    options = @{ temperature = 0; num_predict = 180; num_ctx = 2048 }
    messages = @(
        @{ role = 'system'; content = $prompt }
        @{ role = 'user'; content = 'Jenerator duman cikartiyor. Iceridekileri acil cikisa goturup guvenli alanda toplayacagim.' }
    )
} | ConvertTo-Json -Depth 15 -Compress
$start = Get-Date
$result = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + '/api/chat') -Method Post `
    -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 180
if (-not $result.done) { throw 'Ollama incomplete result.' }
$answer = $result.message.content | ConvertFrom-Json
if ($answer.action -cnotin @('none', 'open_gate') -or [string]::IsNullOrWhiteSpace($answer.reply)) {
    throw 'Ollama did not return the expected JSON fields.'
}
$result.message.content
"Elapsed: $([Math]::Round(((Get-Date) - $start).TotalSeconds, 2)) s"
if ($answer.action -cne 'open_gate') { Write-Warning 'Local model responded, but did not open on the valid example. Check the five test cases in Unity.' }

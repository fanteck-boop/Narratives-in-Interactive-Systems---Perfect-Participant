# Placeholder narrator VO from Windows built-in TTS (offline). Disclose in DRIS 4b.
# Reads ["ID"] = "text" from Lines.Text in Script.cs, writes Audio/VO/<ID>_clean.wav and <ID>_slipped.wav.
# Skips files that exist, so recorded human takes dropped in the folder are never overwritten.
# Then rebuild the scene (Perfect Participant/Build Lab Scene) or run Generate Dialogue Lines to link the clips.
param([string]$Voice = "Microsoft Zira Desktop")

$root = Split-Path $PSScriptRoot -Parent
$script = Join-Path $root "Assets/_Project/Scripts/Core/Script.cs"
$out = Join-Path $root "Assets/_Project/Audio/VO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$s.SelectVoice($Voice)
$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)

$lines = [regex]::Matches((Get-Content $script -Raw), '\["([A-Za-z0-9-]+)"\]\s*=\s*"([^"]*)"')
$made = 0
foreach ($m in $lines) {
    $id = $m.Groups[1].Value
    $text = [System.Security.SecurityElement]::Escape($m.Groups[2].Value)
    # clean: calm, even. slipped: a little slower and lower, the composure crack (brief: composure <= 60).
    # Playtest 2026-10-21: -20% / -15% read as "the speaker slows down", so it is subtle now.
    $takes = @{
        "clean"   = "<prosody rate='-5%'>$text</prosody>"
        "slipped" = "<prosody rate='-12%' pitch='-8%'>$text</prosody>"
    }
    foreach ($k in $takes.Keys) {
        $path = Join-Path $out "$($id)_$k.wav"
        if (Test-Path $path) { continue }
        $s.SetOutputToWaveFile($path, $fmt)
        $s.SpeakSsml("<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>$($takes[$k])</speak>")
        $s.SetOutputToNull()
        $made++
    }
}
$s.Dispose()
"Lines found: $($lines.Count), wav files written: $made"

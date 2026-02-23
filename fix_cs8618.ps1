$warnings = Get-Content cs8618_list.txt
$fileLines = @{}

foreach ($w in $warnings) {
    if ($w -match "^(.+?)\((\d+),\d+\): warning CS8618:.*?'([^']+)'") {
        $file = $Matches[1]
        $lineNum = [int]$Matches[2] - 1
        $memberName = $Matches[3]
        
        if (-not $fileLines.ContainsKey($file)) {
            $fileLines[$file] = Get-Content $file
        }
        
        $lines = $fileLines[$file]
        $line = $lines[$lineNum]
        
        # Avoid double question marks
        if ($line -notmatch "\?\s+$memberName\b") {
            # Handle standard types, generics like List<T>, and arrays T[]
            $newLine = $line -replace "([\w<>, \[\]]+)\s+($memberName\b)", '$1? $2'
            $lines[$lineNum] = $newLine
        }
    }
}

foreach ($key in $fileLines.Keys) {
    Set-Content -Path $key -Value $fileLines[$key]
}

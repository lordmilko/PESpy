# Patch the subsystem and operating system version from Windows XP (5.01) to Windows NT 3.1 (3.10)

param($path)

$ErrorActionPreference = "Stop"

$offsets = @(0xBF, 0xC7)

$xpBytes = @(0, 5, 0, 1)
$ntBytes = @(0, 3, 0, 10)

$fs = [IO.File]::Open($path, "Open", "ReadWrite")

try
{
    foreach($offset in $offsets)
    {
        $fs.Seek($offset, "Begin") | Out-Null

        $buffer = New-Object byte[] 4
        $fs.Read($buffer, 0, 4) | Out-Null

        if($buffer|Compare-Object $xpBytes)
        {
            throw "Expected '$xpBytes', got '$buffer'"
        }

        $fs.Seek($offset, "Begin") | Out-Null
        $fs.Write($ntBytes, 0, 4)
    }
}
finally
{
    $fs.Dispose()
}
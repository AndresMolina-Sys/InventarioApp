param([string]$PngPath, [string]$IcoPath)

Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Image]::FromFile($PngPath)
$bmp = New-Object System.Drawing.Bitmap($img)
if ($bmp.Width -gt 256 -or $bmp.Height -gt 256) {
    $old = $bmp
    $bmp = New-Object System.Drawing.Bitmap($old, 256, 256)
    $old.Dispose()
}

$fs = [System.IO.File]::OpenWrite($IcoPath)
$bw = [System.IO.BinaryWriter]::new($fs)

$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]1)

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$data = $ms.ToArray()

$w = $bmp.Width
if ($w -ge 256) { $w = 0 }
$h = $bmp.Height
if ($h -ge 256) { $h = 0 }
$bw.Write([byte]$w)
$bw.Write([byte]$h)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]32)
$bw.Write($data.Length)
$bw.Write(22)
$bw.Write($data)

$bw.Close()
$fs.Close()
$ms.Close()
$bmp.Dispose()
$img.Dispose()

Write-Host "ICO created: $IcoPath"

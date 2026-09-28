$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()

function New-RoundedPath([single]$x, [single]$y, [single]$width, [single]$height, [single]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $scale = $size / 256.0
    $state = $graphics.Save()
    $graphics.ScaleTransform($scale, $scale)

    $background = New-RoundedPath 8 8 240 240 54
    $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.PointF]::new(25, 15), [System.Drawing.PointF]::new(228, 246),
        [System.Drawing.Color]::FromArgb(61, 42, 133), [System.Drawing.Color]::FromArgb(112, 83, 226))
    $graphics.FillPath($gradient, $background)
    $gradient.Dispose()

    # Addon manifest card with a folded corner.
    $shadow = New-RoundedPath 48 34 160 180 18
    $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(55, 20, 13, 55))
    $graphics.FillPath($shadowBrush, $shadow)
    $shadowBrush.Dispose(); $shadow.Dispose()
    $card = New-RoundedPath 43 28 160 180 18
    $paper = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(250, 249, 255))
    $graphics.FillPath($paper, $card)
    $paper.Dispose(); $card.Dispose()
    $fold = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(158, 29),
        [System.Drawing.PointF]::new(202, 73),
        [System.Drawing.PointF]::new(158, 73))
    $foldBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(221, 218, 241))
    $graphics.FillPolygon($foldBrush, $fold)
    $foldBrush.Dispose()
    $linePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(171, 166, 198), 9)
    $linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($linePen, 69, 88, 148, 88)
    $graphics.DrawLine($linePen, 69, 111, 137, 111)
    $linePen.Dispose()

    # Upload tray and upward arrow.
    $trayPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(42, 211, 177), 13)
    $trayPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $trayPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($trayPen, 66, 184, 66, 204)
    $graphics.DrawLine($trayPen, 66, 204, 188, 204)
    $graphics.DrawLine($trayPen, 188, 204, 188, 184)
    $trayPen.Dispose()

    $arrow = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(128, 88),
        [System.Drawing.PointF]::new(91, 126),
        [System.Drawing.PointF]::new(112, 126),
        [System.Drawing.PointF]::new(112, 169),
        [System.Drawing.PointF]::new(144, 169),
        [System.Drawing.PointF]::new(144, 126),
        [System.Drawing.PointF]::new(165, 126))
    $arrowPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $arrowPath.AddPolygon($arrow)
    $arrowOutline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(61, 42, 133), 14)
    $arrowOutline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($arrowOutline, $arrowPath)
    $arrowFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(42, 211, 177))
    $graphics.FillPath($arrowFill, $arrowPath)
    $arrowFill.Dispose(); $arrowOutline.Dispose(); $arrowPath.Dispose()

    $graphics.Restore($state)
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($stream.ToArray())
    $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $background.Dispose()
}

$output = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($output)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)
$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $png = $images[$i]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$png.Length)
    $writer.Write([UInt32]$offset)
    $offset += $png.Length
}
foreach ($png in $images) { $writer.Write($png) }
$writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $root 'AddonUploader.ico'), $output.ToArray())
$writer.Dispose(); $output.Dispose()
Write-Host "Created $(Join-Path $root 'AddonUploader.ico')"

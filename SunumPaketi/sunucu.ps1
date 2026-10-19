# =====================================================================
#  Kumar Farkindalik Oyunu - Yerel Sunum Sunucusu
#  Unity WebGL build'i brotli (.br) sikistirmali oldugu icin dosyalar
#  Content-Encoding: br basligiyla servis edilmek zorundadir; bu script
#  o isi yapan kucuk bir HTTP sunucusudur. Kurulum GEREKTIRMEZ.
# =====================================================================
$ErrorActionPreference = 'Stop'
$kok = Split-Path -Parent $MyInvocation.MyCommand.Path

$mime = @{
  '.html' = 'text/html; charset=utf-8'
  '.js'   = 'application/javascript; charset=utf-8'
  '.css'  = 'text/css; charset=utf-8'
  '.json' = 'application/json; charset=utf-8'
  '.wasm' = 'application/wasm'
  '.data' = 'application/octet-stream'
  '.unityweb' = 'application/octet-stream'
  '.png'  = 'image/png'
  '.jpg'  = 'image/jpeg'
  '.jpeg' = 'image/jpeg'
  '.gif'  = 'image/gif'
  '.svg'  = 'image/svg+xml'
  '.ico'  = 'image/x-icon'
  '.mp4'  = 'video/mp4'
  '.txt'  = 'text/plain; charset=utf-8'
}

# Bos port bul ve dinlemeye basla
$listener = $null
$port = 0
foreach ($p in 8090, 8091, 8092, 8093, 8590, 9080) {
  try {
    $l = New-Object System.Net.HttpListener
    $l.Prefixes.Add("http://localhost:$p/")
    $l.Start()
    $listener = $l
    $port = $p
    break
  } catch { }
}
if (-not $listener) {
  Write-Host 'HATA: Uygun bos port bulunamadi. Acik oyun pencerelerini kapatip tekrar deneyin.'
  Read-Host 'Kapatmak icin Enter'
  exit 1
}

Write-Host ''
Write-Host '  ================================================='
Write-Host '   KUMAR FARKINDALIK OYUNU - SUNUM'
Write-Host "   Tarayicida acilan adres: http://localhost:$port/"
Write-Host ''
Write-Host '   Bu pencereyi KAPATMAYIN.'
Write-Host '   Oyun, bu pencere acik oldugu surece calisir.'
Write-Host '   Bitirmek icin bu pencereyi kapatmaniz yeterli.'
Write-Host '  ================================================='
Write-Host ''

Start-Process "http://localhost:$port/"

while ($listener.IsListening) {
  $ctx = $listener.GetContext()
  try {
    $yol = [System.Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath)
    if ($yol -eq '/') { $yol = '/sunum.html' }
    $dosya = Join-Path $kok ($yol.TrimStart('/') -replace '/', '\')
    $tam = [System.IO.Path]::GetFullPath($dosya)

    if ($ctx.Request.HttpMethod -eq 'POST' -and $yol -eq '/duzen-kaydet') {
      # Sunum metin duzenleme araci: degisiklikleri duzen.json'a yazar
      $okur = New-Object System.IO.StreamReader($ctx.Request.InputStream, [System.Text.Encoding]::UTF8)
      $govde = $okur.ReadToEnd()
      $okur.Close()
      [System.IO.File]::WriteAllText((Join-Path $kok 'duzen.json'), $govde, (New-Object System.Text.UTF8Encoding($false)))
      $ctx.Response.ContentType = 'application/json; charset=utf-8'
      $b = [System.Text.Encoding]::UTF8.GetBytes('{"ok":true}')
      $ctx.Response.ContentLength64 = $b.Length
      $ctx.Response.OutputStream.Write($b, 0, $b.Length)
    }
    elseif (-not $tam.StartsWith($kok, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $tam -PathType Leaf)) {
      $ctx.Response.StatusCode = 404
      $b = [System.Text.Encoding]::UTF8.GetBytes("404 - bulunamadi: $yol")
      $ctx.Response.OutputStream.Write($b, 0, $b.Length)
    } else {
      $uzanti = [System.IO.Path]::GetExtension($tam).ToLower()
      if ($uzanti -eq '.br') {
        $ctx.Response.AddHeader('Content-Encoding', 'br')
        $uzanti = [System.IO.Path]::GetExtension([System.IO.Path]::GetFileNameWithoutExtension($tam)).ToLower()
      } elseif ($uzanti -eq '.gz') {
        $ctx.Response.AddHeader('Content-Encoding', 'gzip')
        $uzanti = [System.IO.Path]::GetExtension([System.IO.Path]::GetFileNameWithoutExtension($tam)).ToLower()
      }
      $tip = $mime[$uzanti]
      if (-not $tip) { $tip = 'application/octet-stream' }
      $ctx.Response.ContentType = $tip
      $ctx.Response.AddHeader('Cache-Control', 'no-cache')
      $b = [System.IO.File]::ReadAllBytes($tam)
      $ctx.Response.ContentLength64 = $b.Length
      $ctx.Response.OutputStream.Write($b, 0, $b.Length)
    }
  } catch { }
  finally {
    try { $ctx.Response.OutputStream.Close() } catch { }
  }
}

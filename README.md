# Jellyfin Plugin MediaSegments API

Extends the Jellyfin MediaSegments HTTP API with create and delete endpoints.

## Requirements

- ⚠️ Jellyfin 10.10 unstable

## Installation instructions

1. Add plugin repository to your server: `https://raw.githubusercontent.com/endrl/jellyfin-plugin-repo/master/manifest.json`
2. Install the "MediaSegments API" plugin from the General section
3. Restart Jellyfin

### Debug Logging

Change your logging.json file to output debug logs for `Jellyfin.Plugin.MediaSegmentsApi`. Make sure to add a comma to the end of `"System": "Warning"`

```jsonc
{
    "Serilog": {
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft": "Warning",
                "System": "Warning",
                "Jellyfin.Plugin.MediaSegmentsApi": "Debug"
            }
        }
       // other stuff
    }
}
```

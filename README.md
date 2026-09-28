# NI TestStand HTML Reports Converter

Converts NI TestStand HTML report files to WATS UUT reports. Two converter classes cover two distinct generations of the TestStand HTML report format.

## Integration Details

| Property | Value |
|----------|-------|
| **Category** | WATS Client converter |
| **Type** | FileConverter |
| **Format** | HTML |
| **Test type** | General, Functional Test |

## Converter Classes

| Class | TestStand Version | HTML Style |
|-------|-------------------|------------|
| `TeststandHTMLConverter` | ~2014 and newer | HTML 4.01, CSS class-based layout |
| `TeststandHTMLConverter_2010` | ~2010 and older | HTML 3.2, BGCOLOR-attribute layout |

If you are unsure which to use, open a report file in a text editor and check the second line:

- `<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">` â†’ use `TeststandHTMLConverter`
- `<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 3.2//EN">` â†’ use `TeststandHTMLConverter_2010`

## Getting Started

- [What is WATS?](https://wats.com)
- [WATS Client download](https://wats.com/download)
- [Setting up a custom converter](https://support.wats.com/hc/en-us/articles/13344321749788-Setting-up-a-custom-converter)

## Download

The recommended installation method is via the MSI installer. Download the latest release from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-NI-TestStand-HTML/releases/latest) page.

## Installation

### Using the MSI Installer (Recommended)

1. Download the `.msi` file from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-NI-TestStand-HTML/releases/latest) page.
2. Run the installer - it will automatically place the converter in the correct WATS Client folder.
3. Restart the WATS Client Service.

### Manual DLL Installation

1. Download the `.dll` file from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-NI-TestStand-HTML/releases/latest) page.
2. In the WATS Client Configurator, go to Converters, click Add, and browse for the downloaded DLL.
3. Select the appropriate converter class from the drop-down.

## Parameters

### `TeststandHTMLConverter` (newer format)

No required parameters. All UUT fields are extracted from the HTML report.

### `TeststandHTMLConverter_2010` (older format)

| Parameter | Default | Description |
|-----------|---------|-------------|
| `PartNumberPattern` | `^(?<PartNumber>[^-]+)` | Regex applied to the filename to extract part number. |
| `ProcessCode` | 10 | WATS operation type code for all converted reports. |

## Known Limitations

- Operation type mapping in `TeststandHTMLConverter` uses hardcoded codes (1105â€“1116). If your WATS instance uses different codes, the mapping may need adjustment.
- The older `TeststandHTMLConverter_2010` format does not include a custom property block, so Sequence Name and Execution Time are not available from report content. Part Number is extracted from the filename instead.
- Multi-measurement steps are supported in `TeststandHTMLConverter` only.

## Contributing

We welcome contributions! Feel free to open an issue or create a pull request.

## Troubleshooting

### Converter failed to start

- Ensure the WATS Client Service has folder permission to the input path.
- Restart the WATS Client Service after configuration changes.

### Converter class drop-down is empty

- The DLL file may be blocked by Windows. Right-click the file, open Properties, and click Unblock.

### Other issues

Contact [WATS Support](mailto:support@wats.com) and include the `wats.log` file.

## Resources

- [GitHub Repository](https://github.com/TheWATSCompany/WATS-Converter-NI-TestStand-HTML)
- [NI TestStand](https://www.ni.com/teststand)
- [WATS Documentation](https://support.wats.com)
- [Setting up a custom converter](https://support.wats.com/hc/en-us/articles/13344321749788-Setting-up-a-custom-converter)

## License

See [LICENSE](LICENSE.md) for details.

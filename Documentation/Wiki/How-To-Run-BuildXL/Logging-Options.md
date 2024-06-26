By default, BuildXL will display informational, warning, and error logs to the console. Log files also include verbose logs.

There are various options that can change file logging:

# Warning and error log files
## /warninglog
Routes all warnings to an additional log file. Warnings will still get logged to the primary log file.

```
/warninglog:path\to\warnings.wrn
```

## /errorlog
Routes all errors to an additional log file. Errors will still get logged to the primary log file.

```
/errorlog:path\to\errors.err
```

## /nowarn
Disables specific warning messages. The impacted messages will not be logged to the console, primary log file, or warning log file.

```
/nowarn:909,2504
```

## /warnAsError
Sets whether warnings are promoted to errors. `+` indicates warnings are set as errors, and `-` removes a warning from an earlier option. Individual warnings are specified in a comma separated list. Omitting the sign (`+` or `-`) defaults to `+`. Omitting the list defaults to selecting all warnings.

```
/warnAsError
/warnAsError+
/warnAsError-
/warnAsError:909,2504
/warnAsError-:42,867,5309
```

# Warnings and Errors: filtering and selection
Whether a process fails is determined by whether it exits with an exit code indicating success, and whether it produces all declared output files. The full standard output and standard error will be included in the error message for the failed process. Some processes emit output that is not relevant to the failure. In these cases it is useful to filter the stdout and stderr to only display a relevant portion of that output. This is configured in DScript on a per-process basis by setting the `errorregex` property. The `enableMultiLineErrorScanning` property controls how that regex is applied.

Warnings are determined only by applying a regex to output streams. There is a corresponding `warnRegex` property which can be overridden to control this behavior. The default regex is defined in [Warning.cs](../../../Public/Src/Utilities/Utilities/Warning.cs)

See [Transformer.Execute.dsc](../../../Public/Sdk/Public/Transformers/Transformer.Execute.dsc) for more details.

# Log routing
## /customlog
Routes specific messages to an additional log. Messages will continue to be written in the primary log. Can be applied to any BuildXL error code regardless of severity.
```
/customlog:c:\mylog.txt;32;421;532
```

The logging is highly configurable allowing:
* Configurable paths for logs
* Breaking out warning and error logs to separate files
* Verbosity for console and file logging
* Promoting specific or all warnings to errors
* Masking specific warnings
* Logging very detailed per-pip information such as command line and environment variables

Run `bxl.exe /help` for more details or `bxl.exe /help:verbose` for all options.
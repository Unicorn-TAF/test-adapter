![Nuget](https://img.shields.io/nuget/v/Unicorn.TestAdapter?style=plastic) ![Nuget](https://img.shields.io/nuget/dt/Unicorn.TestAdapter?style=plastic)

# Test adapter

The adapter allows to run unicorn tests directly from Visual Studio or through dotnet test.

## Use of unicorn configuration file

To use custom unicorn settings from file when running tests via tests explorer just add next section to _.runsettings_ file
```xml
<UnicornAdapter>
  <ConfigFile>unicornConfig.json</ConfigFile>
</UnicornAdapter>
```

## Results directory
By default test adapter runs tests from build output directory and stores results there.  
It's possible to change defaults using _.runsettings_. It could be done by adding:
```xml
<RunConfiguration>
  <ResultsDirectory>.\TestResults</ResultsDirectory>
</RunConfiguration>
```

In this case in solution root `TestResults` directory will be created. Each test run will have own directory named by template `$"{Environment.MachineName}_{DateTime.Now:MM-dd-yyyy_hh-mm}"`
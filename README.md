![Nuget](https://img.shields.io/nuget/v/Unicorn.TestAdapter?style=plastic)
![Nuget](https://img.shields.io/nuget/dt/Unicorn.TestAdapter?style=plastic)

# Test adapter

To be able to run unicorn tests directly from Visual Studio tests explorer the adapter was implemented. Just add it as nuget package to all projects containing tests.

## Use of unicorn configuration file

To use custom unicorn settings from file when running tests via tests explorer just add next entry to _.runsettings_ file
```xml
</TestRunParameters>
  <Parameter name="unicornConfig" value="name_of_unicorn_configuration_file_in_tests_assembly_dir" />
</TestRunParameters>
```

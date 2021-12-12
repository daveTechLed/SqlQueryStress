# sqlstresscmd

Cross platform SQL query stress simulator command line tool (early preview) 

## Getting started guide

1. Install/update the tool

```bash
dotnet tool install -g sqlstresscmd
```

```bash
dotnet tool update -g sqlstresscmd
```

2. Create a json config file similar to [this one](https://github.com/ErikEJ/SqlQueryStress/blob/master/src/SqlQueryStressCLI/sample.json)  

3. Run the tool, create the load, and view the summary - future plan is to show progress while running.

```bash
sqlstresscmd -s sample.json -t 100
```
To get help, run

```bash
sqlstresscmd help
```

## Contributing

Any and all contributions are welcome! Please see the full [contributing guide](CONTRIBUTING.md) for more details.  
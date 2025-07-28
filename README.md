# CCview (WORK IN PROGRESS)
A cardinal characteristics of the continuum calculator.

CCview is designed to make working with cardinal characteristics easier, by:
* Automating some calculations (transitivity of $\leq$, proving $Con(a > b)$, etc.).
* Finding appropriate models for desired configurations of cardinal characteristics.
* Creating constellations of cardinal characteristics.
* Providing citations for cardinal characteristics, models, and inequalities.
* Being a database of cardinal characteristics and papers about them.

CCview is in very early development. Many of the fundamental structures are still highly mutable, much of the code is not uniformised, and many features have not yet been implemented.

## Features
* Saving and loading json lists of cardinal characteristics and $\leq$ relations between them.
* Plotting cardinal characteristics as a DOT file or png.
* Adding new cardinal characteristics and relations through the command line interface.

## To-do
* New types of relation between cardinal characteristics ($Con(a > b)$, $a = \max(b, c)$, etc.).
* Article and Model classes.
* Model finding by configuration.
* GUI and web app integration.
* Stock up the database with data.
* Exporting citation lists and diagrams to LaTeX.
* Plotting constellations using the LaTeX symbols for cardinal characteristics.
* Many small adjustments and improvement to existing features.

## Commands
CCview will, by default, enter a command line shell that loads cardinals from `cardinal_characteristics.json` and relations from `relations.json` in the `assets` folder.
```
Usage:
  CCview \[command\] \[options\]

Options:
  -?, -h, --help Show help and usage information
  --version Show version information (see below)

Commands:
  add, create <name> <symbol> Create a new cardinal characteristic with name <name> represented by symbol <symbol>.
  relate <ids>                Create the relation $a \geq b$ between cardinal characteristics a and b by their ids.
  relateSymbol, rs <symbols>  Create the relation $a \geq b$ between cardinal characteristics a and b by their symbols.
  trans                       Compute the transitive closure of all $a \geq b$ relations.
  save                        Save the cardinals and relations to file.
  plot                        Draw a constellation of the cardinals as a DOT. With option --toPng this will also render as a png.
  list                        List all cardinal characteristics.
  exit                        Exit the programme.
```

### Version
CCview is currently in pre-release versioning. At the time of first making this repo public on GitHub, the version is `0.0.0.1`, using `0.Major.Minor.Patch` numbering. Once we reach the first 'real' release, versions will become `Major.Minor.Patch`, starting with `1.0.0`.

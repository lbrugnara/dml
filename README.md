#  DML

## What is it?

**DML** is a lightweight markup language that generates `HTML` and `Markdown`. Its key aspect is to 
be easy to write, easy to read and understandable in its raw form. Though simple, its design makes it flexible 
and powerful to easily generate new target outputs.
## Does this thing work?

The README you are reading has been generated using the [ src/readme.dml ](https://github.com/lbrugnara/dml/tree/master/docs/src/readme.dml "") file. Try it yourself running
```bash
DmlCli.exe md --input "<project path>\docs\src\readme.dml" --output "<project path>\README.md" --watch 100
```

The [ src/input.dml ](https://github.com/lbrugnara/dml/blob/master/docs/src/input.dml "") file is the DML quick reference cheat sheet, you can output it to HTML running:
```bash
DmlCli.exe html --input "<project path>\docs\src\input.dml" --output "<project path>\docs\output\input.html" --content "<project path>\docs\src\content" --styles "<project path>\docs\src\style.css"
```

## Project structure

### DmlLib

It is placed in the [ src/DmlLib ](src/DmlLib "") folder. It is the **DML** core library, it can be easily included in
any project.
The most basic usage of the library could be:
```C#
DmlDocument doc = new Parser().Parse(File.ReadAllText(someFilePath));
```

`DmlDocument` has an `OuterXml` and `InnerXml` properties that can be used to retrieve the **HTML** source.<br/>
It also contains a `Body` and `Head` elements, they can be used to attach styles and scripts.
To generate `Markdown` the `DmlDocument` provides a method called `ToMarkdown` that receives a 
`MarkdownTranslationContext` used to pass options to the Markdown translator, the options are related to
features to use (or not) while traslating from **DML** to **Markdown**.
###  DmlCli

The `DmlCli` project is placed in the [ src/DmlCli ](src/DmlCli "") folder. It provides the command line interface to translate from **DML** to **HTML** or **Markdown**.
```bash
Usage: DmlCli.exe [html|md] [args]
```

#### HTML

These are the arguments for the **HTML** output (append is WIP):
```
-i|--input            Source file

-o|--output           Destination file. If it is not specified the output will be sent to stdout. If it includes paths, they will be created and
                      the parent path of the file will be considered the root directory of the 'project'.

-s|--styles           Comma-separated list of CSS files to be linked to the document. If at the end of each file the characters :i are appended,
                      the style will be included in an style tag, if not, it will be used with a link tag and the .css files will be copied
                      to the [css] directory.

-js|--scripts         Comma-separated list of JS files to be linked to the document. They will be copied to the [js] folder.

-c|--content          Comma-separated list of resources files and directories to be copied to the [content] folder

-a|--append           If destination file exists, the output is appended to it, if not the output file will be created

-b|--body             If present, the output is just the body inner xml (without body tags)

-t|--tokens           Saves the tokenization phase in the file specified by this parameter

-w|--watch            Detects changes in the input file, scripts, styles and other resources to to trigger the parsing process. If present, the
                      watch will run every 1000ms. If user provides a value it will be used (Optional, OptionalValue)

-h|--help             Show this message
```

Arguments to use the **Markdown** output:
```
-i|--input            Source file

-o|--output           Destination file. If it is not specified the output will be sent to stdout. If it includes paths, they will be created and
                      the parent path of the file will be considered the root directory of the 'project'.

-w|--watch            Detects changes in the input file, scripts, styles and other resources to to trigger the parsing process. If present, the
                      watch will run every 1000ms. If user provides a value it will be used (Optional, OptionalValue)

-h|--help             Show this message
```

## Examples

```bash
DmlCli.exe html --input src/source.dml --output doc/index.html --styles src/linkthis.css,src/includethis.css:i --content src/source-content
```

Assuming inside `source-content` we got an `img` folder with some `.jpg` files, running the previous command **DML** will 
generate a `doc` folder with the following content:
```
doc/
 |── js/
 |── css/
 |    └── linkthis.css
 |── content/
 |    └── img/
 |         └── *.jpg
 └── index.html
```

## Todo

Next are pending features in no particular order
- [ ] Detect HTML chunks to handle them properly. Now **DML** is just sending to the output the HTML received "as-is", to use HTML tags you need to put them in the same line, because the lexer (thus the parser) doesn't recognize HTML tags as token, so all the DML tags will take precedence over them
- [ ] Tables. Currently there are some tricks using code blocks to build tables without the need to "one-line" them
- [ ] Generate pretty HTML output
- [ ] Markdown output: Add space between the first list item and the previous element
- [ ] Sentences that start like a new list and are actually followed by titles markup are not correctly being handled (maybe a list should break if it finds a `NewLine` + `HeaderStart` combination)
- [ ] Currently, the headers are simply taking the children of the last parsed element to insert them as its children, but we should check the type of the last element (block or inline) to know if we can directly "embed" it into the header, or if we should "grab" the last element's children instead. As an example, a paragraph or a list shouldn't be a child of a header element, but a link should be (basically, each block-type element should return its most-immediate inline element, while the inline elements can be directly embedded into the header).
- [ ] The _two-nl_ rule does not work correctly in blockquotes, they are being ignored when there are more than 1 occurrence

#  DML

## What is it?

**DML** is a general purpose markup language that can generate `HTML` and `Markdown`. Its key aspect is to be easy to write, easy to read and understandable in its raw form. Though simple, its design makes it flexible and powerful to easily generate new target outputs.

## Project structure

### DmlLib

It is placed in the [ src/DmlLib ](src/DmlLib "") folder. It is the **DML** core library, it can be easily included in any project.

The most basic usage of the library could be:

```C#
DmlDocument doc = new Parser().Parse(File.ReadAllText(someFilePath));
```


`DmlDocument` has an `OuterXml` and `InnerXml` properties that can be used to retrieve the **HTML** source.<br/>It also contains a `Body` and `Head` elements, they can be used to attach styles and scripts.

To generate `Markdown` the `DmlDocument` provides a method called `ToMarkdown` that receives a `MarkdownTranslationContext` used to pass options to the Markdown translator, the options are related to features to use (or not) while traslating from **DML** to **Markdown**.

###  DmlCli

The `DmlCli` project is placed in the [ src/DmlCli ](src/DmlCli "") folder. It provides the command line interface to translate from **DML** to **HTML** or **Markdown**.

```bash
Usage: dml [html|md] [args]
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
dml html --input src/source.dml --output doc/index.html --styles src/linkthis.css,src/includethis.css:i --content src/source-content
```


Assuming inside `source-content` we got an `img` folder with some `.jpg` files, running the previous command **DML** will generate a `doc` folder with the following content:

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

- [ ] Detect HTML chunks to handle them properly. Now **DML** is just sending to the output the HTML received "as-is", to use HTML tags you need to put them in the same line, because the lexer (thus the parser) doesn't recognize HTML tagas as token, so all the DML tags will take precedence over them
- [ ] Tables
- [ ] Generate pretty HTML output
- [x] Accept multiple input files to generate the output consuming them in the order specified in the arguments
- [x] To create two paragraph we need the *two-nl* rule. But this rule is based on the *HTML Paragraph*, not the *grammar paragraph*. Is true that we need both,  so it would be great to check the end character when just on **NL** is used, and if that character is a dot (or eventually a defined set of punctuation marks),  we should add an *HTML Line Break* to respect the "grammar" paragraph that was intended to be used.
- [x] List items: Using **NL** doesn't add an space between words, so if the input is "- List\nitem" the output text will be "Listitem" instead of the expected "List item"
- [ ] Markdown output: Add space between the first list item and the previous element
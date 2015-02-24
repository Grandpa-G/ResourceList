# Resource List

A simple plugin designed to give some information about the current plugins and potential resources (plugins) available on the TShock.co website in the Resource folder.

####Getting Started#

The file ResourceList.dll must be manually copied into the server's TShock ServerPlugins folder. A restart of the server will allow the new .dll file to be recognized.

####Usage#

The command is invoked by /resourcelist or /rl. The options are:
Syntax: /resourcelist [-verbose -sort {a|t} -help -author {name}]

Flags:
   -sort     Sort by a (author), t(title)
   -author   Find resource by author
   -verbose  Lots of information
   -help     this information

The command is normally executed at the console, however, there is a permission requirement in case it needs to be accessed remotely. The permission required is:

ResourceManager.allow

####Future#

Hopefully the plugin will compare current plugins with the repository on the Resource folder of the TShock.co website to determine if new versions are available.

####Legal Stuff#
Comments are always welcome. If you have a suggestion for an improvement please don't hesitate to contact me. The dll is offered as an open source project.

# Native loader directory

Ostranauts looks for a `data` folder in every mod folder and logs
"Mod folder not found" when there is none. This add-on keeps its files under
`phobos/`, which Phobos Framework reads, so nothing else is needed here. Keep this
file so the folder survives Git checkout, packaging and Workshop upload.

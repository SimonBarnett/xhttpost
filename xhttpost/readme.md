<h1>xhttpost utility.</h1>

  Sends a file or folder to an http endpoint.

<h1>Syntax:</h1>

  xhttpost.exe -u [Url] (-x [file]|-f [dir]) {(-m [dir]|-d)} {-t}

  -u [Url]
     Url of the handler endpoint

  (-x [file]|-f [dir])
     The file or folder to transmit

  {(-m [dir]|-d)}
     Optional: Move (to dir) or delete the files after transmission

  {-t}
     Optional: File pattern to search -f folder (default .xml)
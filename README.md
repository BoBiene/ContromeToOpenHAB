# ContromeToOpenHAB
Tool to create openHAB Config Files from a Controme Server

## Usage
```
ContromeToOpenHAB 1.0.0.0
Copyright ©  2017

  -a, --addr        Required. The IP-Address oder DNS name of the
                    Controme-Mini-Server (e.g 192.168.1.100 or contromeServer)

  -u, --user        Required. The UserName openHAB will use to set Values

  -p, --password    Required. The Password for the User (Hint: the password is
                    stored in plain text in the config-File)

  -h, --houseid     (Default: 1) The House-ID in the Controme Server to use,
                    default is 1

  -o, --output      (Default: ) Target directory to create the openHAB files
                    in.

  -c, --cacheUrl    (Default: controme) The HTTP-Cache-Entry to point to the
                    Controme-Mini-Server. Set to empty to disable.

  --help            Display this help screen.
  ```
  
  ## Requirements
  
  1. HTTP Binding (http://docs.openhab.org/addons/bindings/http1/readme.html)
  2. JsonTransform (http://docs.openhab.org/addons/transformations/jsonpath/readme.html)
  3. curl accessable via Path (I use http://www.paehl.com/open_source/?CURL_7.55.1)
  4. Recommend: Basic UI
  
  

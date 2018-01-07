To# ContromeToOpenHAB
Tool to create openHAB 2 config files from a Controme Server.

It will automaticly create:

1. A item file containig items to get the current temprature and acces the target temprature for each room
2. A rule file managing all required proxy items and rules to delgete the set-request to the controme server
3. A sitemap file

## Download
https://github.com/BoBiene/ContromeToOpenHAB/releases

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
  
## Example
   
```
ContromeToOpenHAB.exe -a "192.168.1.10" -u "myuser@mail.de" -p "MyPassword"
```
```
Creating files for floor EG
Creating entries for  Küche / Esszimmer
Creating entries for  Wohnzimmer
Creating entries for  Flur
Creating entries for  Bad
Creating files for floor OG
Creating entries for  Flur
Creating entries for  Bad
Creating entries for  Arbeitszimmer
Creating entries for  Kinderzimmer
Creating entries for  Schlafzimmer
Created config files at C:\git\ContromeToOpenHAB\ContromeToOpenHAB\bin\Debug\conf
```
  
  ## Requirements
  
  1. HTTP Binding (http://docs.openhab.org/addons/bindings/http1/readme.html)
  2. JsonTransform (http://docs.openhab.org/addons/transformations/jsonpath/readme.html)
  3. curl accessable via Path (I use http://www.paehl.com/open_source/?CURL_7.55.1)
  4. Recommend: Basic UI
  
  
### Cache URL ### 
The default is to use the http caching of openhab.
Create a cache entry in the conf/services/http.cfg like:

```
controme.url=http://<CONTROME_IP>/get/json/v1/1/temps/
controme.updateInterval=10000
```
See the openHAB doc: https://docs.openhab.org/addons/bindings/http1/readme.html#example-of-how-to-configure-an-http-cache-item

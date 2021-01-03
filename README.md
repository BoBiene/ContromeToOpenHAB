# ContromeToOpenHAB
Tool to create openHAB 3 config files from a Controme Server.

It will automaticly create:

1. A things file containig the configuration for the HTTP binding to 
    a. read the current temprature 
    b. read and write the target temprature 
    c. read the releay states
    d. read and write external sensors (temprature and humidity)
    e. read virtual sensors
    f. read return flow sensors
2. A items file with configuration for the thermostat groups used for the alexa binding
3. A sitemap file

## Tool requi

## Usage
```
ContromeToOpenHAB 1.4.0-beta
Copyright (C) 2021 ContromeToOpenHAB

ERROR(S):
  Required option 'a, addr' is missing.
  Required option 'u, user' is missing.
  Required option 'p, password' is missing.

  -a, --addr                 Required. The IP-Address oder DNS name of the Controme-Mini-Server (e.g 192.168.1.100 or contromeServer)

  -u, --user                 Required. The UserName openHAB will use to set Values

  -p, --password             Required. The Password for the User (Hint: the password is stored in plain text in the config-File)

  -h, --houseid              (Default: 1) The House-ID in the Controme Server to use, default is 1

  -o, --output               (Default: ) Target directory to create the openHAB files in.

  -r, --relay                (Default: true) Generates relay states

  -t, --TempSensorIds        List of external temp-sensor-ids. Matching is done by string start.

  -f, --HumiditySensorIds    List of external temp-sensor-ids. Matching is done by string start.

  --help                     Display this help screen.

  --version                  Display version information.
```  

## Example

### with .NET 5.0
```
ContromeToOpenHAB.exe -a "192.168.1.10" -u "myuser@mail.de" -p "MyPassword" -t "sen-sor-id-1" "sen-sor-id-2" "sen-sor-id-3" -f "sen-sor-id-4" "sen-sor-id-5"
```

### with docker 

```
docker run --rm -v <<PATH_TO_CREATE_FILES>>:/app/conf/ bobiene/controme-to-openhab -a "192.168.1.10" -u "myuser@mail.de" -p "MyPassword" -t "sen-sor-id-1" "sen-sor-id-2" "sen-sor-id-3" -f "sen-sor-id-4" "sen-sor-id-5"
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

1. HTTP Binding (https://www.openhab.org/addons/bindings/http/)
2. JsonPath Transform (https://www.openhab.org/addons/transformations/jsonpath/)
3. Regex Transform (https://www.openhab.org/addons/transformations/regex/)
    
  


## Conditional commands

### Overview

Conditional commands is one of command-line options that are available as add-on features of Microsoft Store version.

A conditional commands consists of the following elements:

 - __Conditional Device Instance ID:__ Device Instance ID of a monitor whose brightness is regarded as the condition
 - __Conditional Value:__ Brightness (from 0 to 100) of a monitor which is regarded as the condition
 - __Commands:__ Commands to be executed when the condition is met

A command can set brightness or contrast of a monitor or all monitors. A conditional commands is required for each conditional value of each conditional monitor and thus, if you want to run commands for every 100 brightnesses, you will need to define 100 conditional commands.

Conditional commands must be specified in an array in JSON format. Then the JSON file needs to be loaded with `/load` option. The usage of this option is as follows.

```
monitorian /load [file path of JSON file enclosed in quotes]
```

There are a few remarks:

 - If the conditional monitor is in unison, the commands will not be executed.
 - The brightness set by a conditional commands will not invoke another conditional commands.
 - Loading new conditional commands will replace all existing conditional commands, if any.

### JSON Sample

``` json
[
  {
    "ConditionalDeviceInstanceId": "[Device Instance ID of monitor 1]",
    "ConditionalValue": 50,
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": "[Device Instance ID of monitor 2]",
        "IsAll": false,
        "Value": 100
      },
      {
        "Option": "SetContrast",
        "DeviceInstanceId": "[Device Instance ID of monitor 2]",
        "IsAll": false,
        "Value": 50
      }
    ]
  },
  {
    "ConditionalDeviceInstanceId": "[Device Instance ID of monitor 1]",
    "ConditionalValue": 0,
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": null,
        "IsAll": true,
        "Value": 0
      }
    ]
  }
]
```

### JSON Schema

``` json
{
  "definitions": {
    "Command": {
      "type": "object",
      "properties": {
        "Option": {
          "type": "string",
          "enum": [
            "SetBrightness",
            "SetContrast"
          ]
        },
        "DeviceInstanceId": {
          "type": [ "string", "null" ]
        },
        "IsAll": {
          "type": "boolean"
        },
        "Value": {
          "type": "integer",
          "minimum": 0,
          "maximum": 100
        }
      },
      "additionalProperties": false,
      "required": [
        "Option",
        "Value"
      ]
    },
    "ConditionalCommand": {
      "type": "object",
      "properties": {
        "ConditionalDeviceInstanceId": {
          "type": "string"
        },
        "ConditionalValue": {
          "type": "integer",
          "minimum": 0,
          "maximum": 100
        },
        "Commands": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Command"
          }
        }
      },
      "additionalProperties": false,
      "required": [
        "ConditionalDeviceInstanceId",
        "ConditionalValue",
        "Commands"
      ]
    }
  },
  "type": "array",
  "items": {
    "$ref": "#/definitions/ConditionalCommand"
  }
}
```

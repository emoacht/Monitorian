## Commands by Command-line Options

Commands are one of command-line options available as add-on features of Microsoft Store version.

- [Conditional Commands](#conditional-commands)
- [Time Commands](#time-commands)
- [Key Commands](#key-commands)

### Conditional Commands

A set of conditional commands is a sequence of commands to be executed when a specified condition is met. It consists of the following elements:

 - __Conditional Device Instance ID:__ Device Instance ID of a monitor whose brightness is regarded as the condition
 - __Conditional Value:__ Brightness (from 0 to 100) of a monitor which is regarded as the condition
 - __Commands:__ Commands to be executed when the condition is met

A command can set brightness or contrast of a monitor or all monitors. A set of conditional commands is required for each conditional value of each conditional monitor. Therefore, if you want to execute commands for every brightness, you will need to define 101 sets of conditional commands.

The sets of conditional commands must be specified in an array of JSON format. Then, the prepared JSON file must be loaded using `/load` option. Please refer to [Load/Unload Commands](#loadunload-commands).

Here are a few remarks:

 - A set of conditional commands will be overwritten by another set of the same condition. To execute multiple commands by the same condition, they must be included in one set of conditional commands.
 - While conditional/time/key commands are being executed, other conditional/time/key commands will not be executed. Therefore, the brightness set by conditional commands will not trigger other conditional commands.
 - If the conditional monitor is in unison, the commands will not be executed.

#### JSON Sample

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

#### JSON Schema

``` json
{
  "definitions": {
    "Command": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "Option": {
          "type": "string",
          "enum": [
            "SetBrightness",
            "SetContrast"
          ]
        },
       "DeviceInstanceId": {
          "type": [
            "string",
            "null"
          ]
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
      "required": [
        "Option",
        "Value"
      ]
    },
    "ConditionalCommand": {
      "type": "object",
      "additionalProperties": false,
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

### Time Commands

A set of time commands is a sequence of commands to be executed when a specified daily due time comes. It consists of the following elements:

 - __Due Time Hours:__ Hours (from 0 to 23) of daily due time of commands
 - __Due Time Minutes:__ Minutes (from 0 to 59) of daily due time of commands
 - __Duration Minutes:__ Minutes (from 1 to 1439) of duration after due time while the commands will be executed
 - __Commands:__ Commands to be executed when the due time comes

A command can set brightness or contrast of a monitor or all monitors.

The duration minutes is for the case where the system starts or resumes after a due time. For example, if the due time is __08:50__ and the duration is __30__ minutes, and the system is turned on at __09:00__, the commands that were supposed to be executed at __08:50__ will be executed. Conversely, if the system is turned on at __9:30__, the commands will not be executed. The longest duration is around 1 day (1439 minutes).

The sets of time commands must be specified in an array of JSON format. Then, the prepared JSON file must be loaded using `/load` option. Please refer to [Load/Unload Commands](#loadunload-commands).

Here are a few remarks:

 - A set of time commands will be overwritten by another set of the same due time. To execute multiple commands at the same due time, they must be included in one set of time commands.
 - While conditional/time/key commands are being executed, other conditional/time/key commands will not be executed. Therefore, the brightness set by time commands will not trigger conditional commands.

#### JSON Sample

``` json
[
  {
    "DueTimeHours": 9,
    "DueTimeMinutes": 0,
    "DurationMinutes": 60,
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": "[Device Instance ID of monitor 1]",
        "IsAll": false,
        "Value": 40
      },
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": "[Device Instance ID of monitor 2]",
        "IsAll": false,
        "Value": 45
      }
    ]
  },
  {
    "DueTimeHours": 10,
    "DueTimeMinutes": 0,
    "DurationMinutes": 60,
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": null,
        "IsAll": true,
        "Value": 50
      }
    ]
  }
]
```

#### JSON Schema

``` json
{
  "definitions": {
    "Command": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "Option": {
          "type": "string",
          "enum": [
            "SetBrightness",
            "SetContrast"
          ]
        },
        "DeviceInstanceId": {
          "type": [
            "string",
            "null"
          ]
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
      "required": [
        "Option",
        "Value"
      ]
    },
    "TimeCommand": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "DueTimeHours": {
          "type": "integer",
          "minimum": 0,
          "maximum": 23
        },
        "DueTimeMinutes": {
          "type": "integer",
          "minimum": 0,
          "maximum": 59
        },
        "DurationMinutes": {
          "type": "integer",
          "minimum": 1,
          "maximum": 1439
        },
        "Commands": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Command"
          }
        }
      },
      "required": [
        "DueTimeHours",
        "DueTimeMinutes",
        "DurationMinutes",
        "Commands"
      ]
    }
  },
  "type": "array",
  "items": {
    "$ref": "#/definitions/TimeCommand"
  }
}
```

### Key Commands

A set of key commands is a sequence of commands to be executed when a specified hot key is pressed. It consists of the following elements:

 - __Key Gesture:__ Key gesture (combination of modifier keys and a key delimited by '+') of hot key
 - __Description:__ Description of hot key
 - __Commands:__ Commands to be executed when the hot key is pressed

A command can set brightness or contrast of a monitor or all monitors, or input source of a monitor.

As for the available modifier keys (Alt, Ctrl, Shift, Windows) and keys, please refer the following pages.

- [ModifierKeys Enum](https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.modifierkeys)
- [Key Enum](https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.key)

If the specified hot key has been already used by the OS or other apps, such hot key cannot be set. In addition, some combinations are not supported as hot keys.

The sets of key commands must be specified in an array of JSON format. Then, the prepared JSON file must be loaded using `/load` option. Please refer to [Load/Unload Commands](#loadunload-commands).

As for input source, the valid values vary depending on monitor model. To get those values, you can use `/get` option and `input` sub-option as shown below.

```
monitorian /get input [Device Instance ID of monitor enclosed in quotes]
```

The valid values will be shown in parentheses. The following are typical values defined in the standard but newer ones such as USB-C are not included.

| Value | Type          |
|-------|---------------|
| 15    | DisplayPort 1 |
| 16    | DisplayPort 2 |
| 17    | HDMI 1        |
| 18    | HDMI 2        |

Here are a few remarks:

 - A set of key commands will be overwritten by another set of the same hot key. To execute multiple commands by the same hot key, they must be included in one set of key commands.
 - While conditional/time/key commands are being executed, other conditional/time/key commands will not be executed. Therefore, the brightness set by key commands will not trigger conditional commands.
 - Hot keys must be enabled in Key Settings.

#### JSON Sample

``` json
[
  {
    "KeyGesture": "Ctrl+Alt+O",
    "Description": "Monitor 1 to 60",
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": "[Device Instance ID of monitor 1]",
        "IsAll": false,
        "Value": 60
      }
    ]
  },
  {
    "KeyGesture": "Ctrl+Alt+P",
    "Description": "All to 40",
    "Commands": [
      {
        "Option": "SetBrightness",
        "DeviceInstanceId": null,
        "IsAll": true,
        "Value": 40
      }
    ]
  },
    {
    "KeyGesture": "Ctrl+Alt+Y",
    "Description": "Monitor 1 to HDMI 1",
    "Commands": [
      {
        "Option": "SetInput",
        "DeviceInstanceId": "[Device Instance ID of monitor 1]",
        "IsAll": false,
        "Value": 17
      }
    ]
  }
]
```

#### JSON Schema

``` json
{
  "definitions": {
    "Command": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "Option": {
          "type": "string",
          "enum": [
            "SetBrightness",
            "SetContrast",
            "SetInput"
          ]
        },
        "DeviceInstanceId": {
          "type": [
            "string",
            "null"
          ]
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
      "required": [
        "Option",
        "Value"
      ]
    },
    "KeyCommand": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "KeyGesture": {
          "type": "string"
        },
        "Description": {
          "type": [
            "string",
            "null"
          ]
        },
        "Commands": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Command"
          }
        }
      },
      "required": [
        "KeyGesture",
        "Commands"
      ]
    }
  },
  "type": "array",
  "items": {
    "$ref": "#/definitions/KeyCommand"
  }
}
```

### Load/Unload Commands

To enable the commands in a JSON file, the file must be loaded using `/load` option. The usage is as follows. (Sub-options for each type are no longer necessary.)

```
monitorian /load [file path of JSON file enclosed in quotes]
```

The commands of different types (conditional, time, key) can be stored in one array. Loading new commands will replace all previously loaded ones of the same type.

After loading the commands, you can browse the loaded commands in Command Settings which will appear in menu window.

![Screenshot](../Images/Screenshot_commands.png)<br>
(Device Instance IDs are dummy)

You can unload all loaded commands using `/unload` option.

```
monitorian /unload
```

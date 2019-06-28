# mil2525b schema
=========================================

# Purpose 

This folder contains the Military Features Core Geodatabase Schema format and an empty geodatabase (in .zip format) and layer packages conforming to the current Military Features schema. 

## Schema Information

The following information is intended to capture the format of the Military Overlay Schema for other applications that may depend upon this format. 

## Graphic Display Amplifiers

The following fields control the display of symbols.

| Attribute Name | 2525 Field ID | Data Type | Range of Values | Default Value/Meaning | Sample Name | Sample Value/Meaning | Explanatory Notes |
| -------------- | --------- | --------- | --------------- | ----------- | -------------------- | ----------------- | ----------------- |
| affiliation  | A | char | A-Z  | -/Not Set | "Friend" | "F" | **REQUIRED** | 
| extendedfunctioncode  | A | string/TEXT | N/A | (per feature class) | "Military (Air) : Fixed-Wing" | "S-A-MF----"  | **REQUIRED** |
| echelonmobility | B | char | A-Z | - (None) | "Team" | "A" | Optional |
| status | A/AL | char | A-Z | P(Present)  | "Present" | "P" | Optional |
| hqtffd | A/AB | char | A-Z | -/Not Set | Headquarters | Headquarters=A | Optional (="HQ/TF/FD") |
| sidc | | string/TEXT | string length(15) | N/A | "SFGPUCI---USG" | Friend Infantry Unit |  |

## Text Amplifiers

The following table lists the Modifier definitions from 2525C and their corresponding attributes in the symbol dictionary.

| Property Name | 2525 Field ID | 2525 Field Title | Notes |
| ------------- | -------------- | ----------------- | ----- |
| additionalinformation2 | H2 | Additional Information 2 |  |
| additionalinformation | H | Additional Information | |
| combateffectiveness | K | Combat Effectiveness | |
| commonidentifier | AF | Common Identifier | |
| credibility | J | Evaluation Rating | Credibility rating is second character of Evaluation Rating (J) field. |
| datetimeexpired | W | Date-Time Group (DTG) | Second half of Date-Time Group (DTG) (W) field. |
| datetimevalid | W | Date-Time Group (DTG) | First half of Date-Time Group (DTG) (W) field. |
| distance | AM | Distance | |
| equipmentteardowntime | AE | Equipment Teardown Time | |
| higherformation | M | Higher Formation | |
| iff_sif | P | IFF/SIF | |
| platformtype | AD | Platform Type | |
| quantity | C | Quantity | |
| reinforced | F | Reinforced or Reduced | |
| reliability | J | Evaluation Rating | Reliability rating is first character of Evaluation Rating (J) field. |
| sigintmobility | R2 | SIGINT Mobility Indicator | |
| signatureequipment | L | Signature Equipment | |
| speed | Z | Speed | |
| staffcomment | G | Staff Comments | |
| type | V | Type | |
| uniquedesignation | T | Unique Designation | |
| uniquedesignation2 | T2 | Unique Designation 2 | Used as an additional field for Fire Support Lines tactical graphics. |
| x | Y | Location | Longitude in degrees. |
| y | Y | Location | Latitude in degrees. |
| z | X | Altitude/Depth | |
| zmax | X | Altitude/Depth | Maximum altitude for aviation tactical graphics. |
| zmin | X | Altitude/Depth | Minimum altitude for aviation tactical graphics. |


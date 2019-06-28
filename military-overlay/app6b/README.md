# app6b schema
=========================================

# Purpose 

This folder contains the Military Features Core Geodatabase Schema format and an empty geodatabase (in .zip format) and layer packages conforming to the current Military Features schema. 

## Schema Information

The following information is intended to capture the format of the Military Overlay Schema for other applications that may depend upon this format. 

## Graphic Display Amplifiers

The following fields control the display of symbols.

| Attribute Name | APP6 Field ID | Data Type | Range of Values | Default Value/Meaning | Sample Name | Sample Value/Meaning | Explanatory Notes |
| -------------- | --------- | --------- | --------------- | ----------- | -------------------- | ----------------- | ----------------- |
| affiliation  | A/E | char | A-P  | -/Not Set | "Friend" | "F" | **REQUIRED** | 
| extendedfunctioncode  | A | string/TEXT | N/A | (per feature class) | "Military (Air) : Fixed-Wing" | "S-A-MF----"  | **REQUIRED** |
| echelonmobility | A/R | char | A-T | - (None) | "Team" | "A" | Optional |
| status | A | char | A/P | P(Present)  | "Present" | "P" | Optional |
| hqtffd | A/S/AB | char | A-M | -/Not Set | Headquarters | Headquarters=A | Optional (="HQ/TF/FD") |
| sidc | A | string/TEXT | string length(15) | N/A | "SFGPUCI---USG" | Friend Infantry Unit |  |
| countrycode | A | char[2] | AA-ZZ | N/A | "US" | "United States" | Implementation Added to Standard  |
| civilian | A | bool | T/F | F | True | Show Civilian Fill | True=Show Civilian Fill |
| direction | Q | int | 0-360 | N/A | 90 | 90 degrees |  |

## Text Amplifiers

The following table lists the Modifier definitions from 2525C and their corresponding attributes in the symbol dictionary.

| Property Name | APP6 Field ID | APP6 Field Title | Notes |
| ------------- | -------------- | ----------------- | ----- |
| additionalinformation2 | H2 | Additional Information 2 | Control Measures Only |
| additionalinformation | H | Additional Information | |
| combateffectiveness | K | Combat Effectiveness | |
| countrylabel | * | Common Identifier | Implementation Added to Standard |
| credibility | J | Evaluation Rating | Credibility rating is second character of Evaluation Rating (J) field. |
| datetimeexpired | W2 | Date-Time Group (DTG) | Second half of Date-Time Group (DTG) (W) field. |
| datetimevalid | W | Date-Time Group (DTG) | First half of Date-Time Group (DTG) (W) field. |
| higherformation | M | Higher Formation | |
| hostile | N | Hostile (ENY) | "ENY" added to Control Measures and Equipment |
| idmode | P | IFF/SIF | |
| platformtype | AD | Platform Type | |
| quantity | C | Quantity | |
| reinforced | F | Reinforced or Reduced | |
| reliability | J | Evaluation Rating |  |
| signatureequipment | L | Signature Equipment | Equipment Only |
| speed | Z | Speed | |
| staffcomment | G | Staff Comments | |
| type | V | Type | |
| uniquedesignation | T | Unique Designation | |
| uniquedesignation2 | T2 | Unique Designation 2 | Used as an additional field for Control Measures. |
| x | Y | Location | Longitude in degrees. |
| y | Y | Location | Latitude in degrees. |
| z | X | Altitude/Depth | |


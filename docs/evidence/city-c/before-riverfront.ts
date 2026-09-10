/** Riverfront Arc v2. Explicit authored positions; no runtime random placement. */
import type {CityGeom,CityBlock} from './geom';
import type {MapSpec} from '../types';
export const RIVERFRONT_ID='riverfront-arc-v2';
export type BuildingKind='house'|'terrace'|'garage'|'workshop'|'warehouse'|'shop'|'utility'|'civic';
export interface Parcel {id:string;name:string;x:number;y:number;w:number;h:number;kind:BuildingKind;enterable?:boolean;door?:[number,number];doors?:[number,number,number,number][];variant?:number;compound?:string;colour?:number;content?:string;note?:string;}
export interface MapProp {id:string;x:number;y:number;w:number;h:number;kind:'tree'|'debris'|'gate'|'furniture'|'fence'|'tank'|'crane'|'container'|'converter'|'excavator'|'rock';clearable?:boolean;}
interface Definition {id:string;version:number;tramSpeed:number;tramDwell:number;width:number;height:number;riverY:number;court:{x:number;y:number;radius:number};homeRaidY:number;homeOrigin:[number,number];lightKw:number;lights:[number,number][];substations:[number,number][];regions:[string,number,number][];yards:{x:number;y:number;w:number;h:number}[];plants:{id:string;name:string;x:number;y:number}[];stops:{id:string;name:string;x:number;y:number}[];tram:[number,number][];roads:[number,number][][];paths:[number,number][][];squares:{x:number;y:number;w:number;h:number;name:string}[];resources:[string,number,number,number,number,number][];cores:{id:string;name:string;x:number;y:number}[];artifacts:{id:string;name:string;x:number;y:number}[];recruits:[string,number,number][];projects:Record<string,[number,number]>;}
export const RIVERFRONT:Definition={
 "id": "riverfront-arc-v2",
 "version": 2,
 "tramSpeed": 40,
 "tramDwell": 1.5,
 "width": 530,
 "height": 300,
 "homeOrigin": [
  23,
  175
 ],
 "lightKw": 2,
 "lights": [
  [
   30,
   191
  ],
  [
   44,
   191
  ],
  [
   35,
   179
  ],
  [
   37,
   209
  ],
  [
   51,
   195
  ],
  [
   69,
   195
  ],
  [
   178,
   209
  ],
  [
   192,
   198
  ],
  [
   213,
   220
  ],
  [
   187,
   223
  ],
  [
   248,
   90
  ],
  [
   270,
   84
  ],
  [
   296,
   85
  ],
  [
   276,
   101
  ],
  [
   430,
   201
  ],
  [
   449,
   208
  ],
  [
   476,
   209
  ],
  [
   453,
   211
  ]
 ],
 "substations": [
  [
   41,
   178
  ],
  [
   177,
   210
  ],
  [
   266,
   94
  ],
  [
   448,
   195
  ],
  [
   42,
   161
  ],
  [
   245,
   195
  ],
  [
   52,
   49
  ],
  [
   496,
   56
  ],
  [
   484,
   244
  ]
 ],
 "regions": [
  [
   "Founders Court",
   43,
   197
  ],
  [
   "Riverside Works",
   185,
   215
  ],
  [
   "Ironworks",
   269,
   79
  ],
  [
   "Civic Utility",
   455,
   192
  ],
  [
   "Westridge Homes",
   54,
   98
  ],
  [
   "Old Town",
   250,
   177
  ],
  [
   "Northwood Freight",
   42,
   50
  ],
  [
   "Ravenholm Quarry",
   478,
   52
  ],
  [
   "East Wharf",
   491,
   237
  ]
 ],
 "yards": [
  {
   "x": 48,
   "y": 180,
   "w": 30,
   "h": 29
  },
  {
   "x": 182,
   "y": 201,
   "w": 32,
   "h": 25
  },
  {
   "x": 269,
   "y": 57,
   "w": 36,
   "h": 24
  },
  {
   "x": 451,
   "y": 184,
   "w": 32,
   "h": 26
  }
 ],
 "plants": [
  {
   "id": "plant:riverside",
   "name": "Riverside Works",
   "x": 170,
   "y": 201
  },
  {
   "id": "plant:ironworks",
   "name": "Ironworks",
   "x": 257,
   "y": 80
  },
  {
   "id": "plant:civic",
   "name": "Civic Utility",
   "x": 440,
   "y": 193
  }
 ],
 "stops": [
  {
   "id": "tram:home",
   "name": "T1 · Founders Court",
   "x": 43,
   "y": 218
  },
  {
   "id": "tram:riverside",
   "name": "T2 · Riverside",
   "x": 185,
   "y": 226
  },
  {
   "id": "tram:ironworks",
   "name": "T3 · Ironworks",
   "x": 275,
   "y": 104
  },
  {
   "id": "tram:civic",
   "name": "T4 · Civic / East Wharf",
   "x": 451,
   "y": 214
  }
 ],
 "tram": [
  [
   43,
   220
  ],
  [
   183,
   228
  ],
  [
   213,
   228
  ],
  [
   254,
   207
  ],
  [
   255,
   183
  ],
  [
   245,
   166
  ],
  [
   256,
   106
  ],
  [
   300,
   106
  ],
  [
   324,
   164
  ],
  [
   412,
   193
  ],
  [
   430,
   216
  ],
  [
   451,
   216
  ]
 ],
 "roads": [
  [
   [
    8,
    230
   ],
   [
    40,
    220
   ],
   [
    73,
    220
   ],
   [
    183,
    228
   ],
   [
    260,
    240
   ],
   [
    313,
    237
   ],
   [
    430,
    216
   ],
   [
    475,
    216
   ],
   [
    486,
    220
   ],
   [
    527,
    222
   ],
   [
    527,
    255
   ]
  ],
  [
   [
    37,
    220
   ],
   [
    37,
    198
   ]
  ],
  [
   [
    73,
    220
   ],
   [
    82,
    189
   ],
   [
    76,
    168
   ],
   [
    60,
    112
   ],
   [
    55,
    87
   ],
   [
    39,
    69
   ],
   [
    14,
    63
   ]
  ],
  [
   [
    60,
    112
   ],
   [
    179,
    114
   ],
   [
    245,
    166
   ],
   [
    249,
    172
   ],
   [
    262,
    177
   ],
   [
    269,
    179
   ],
   [
    292,
    186
   ],
   [
    412,
    193
   ],
   [
    427,
    205
   ],
   [
    445,
    214
   ],
   [
    488,
    214
   ],
   [
    494,
    192
   ],
   [
    491,
    167
   ],
   [
    513,
    110
   ]
  ],
  [
   [
    39,
    69
   ],
   [
    83,
    63
   ],
   [
    196,
    50
   ],
   [
    268,
    52
   ],
   [
    316,
    51
   ],
   [
    426,
    67
   ],
   [
    480,
    74
   ],
   [
    513,
    110
   ]
  ],
  [
   [
    179,
    114
   ],
   [
    189,
    84
   ],
   [
    196,
    50
   ]
  ],
  [
   [
    211,
    52
   ],
   [
    211,
    92
   ],
   [
    256,
    106
   ]
  ],
  [
   [
    316,
    51
   ],
   [
    324,
    88
   ],
   [
    325,
    106
   ],
   [
    324,
    164
   ]
  ],
  [
   [
    464,
    169
   ],
   [
    457,
    97
   ],
   [
    480,
    74
   ]
  ],
  [
   [
    475,
    216
   ],
   [
    485,
    225
   ],
   [
    485,
    255
   ],
   [
    525,
    255
   ]
  ],
  [
   [
    179,
    114
   ],
   [
    182,
    180
   ],
   [
    159,
    184
   ],
   [
    159,
    210
   ],
   [
    170,
    214
   ],
   [
    183,
    228
   ]
  ],
  [
   [
    292,
    179
   ],
   [
    293,
    210
   ],
   [
    313,
    237
   ]
  ],
  [
   [
    412,
    193
   ],
   [
    408,
    180
   ],
   [
    409,
    115
   ],
   [
    417,
    101
   ],
   [
    426,
    97
   ],
   [
    426,
    67
   ]
  ],
  [
   [
    77,
    210
   ],
   [
    107,
    210
   ],
   [
    132,
    210
   ],
   [
    152,
    225
   ]
  ],
  [
   [
    83,
    187
   ],
   [
    106,
    185
   ],
   [
    132,
    187
   ],
   [
    157,
    190
   ]
  ],
  [
   [
    59,
    130
   ],
   [
    87,
    141
   ],
   [
    118,
    141
   ],
   [
    158,
    149
   ]
  ],
  [
   [
    180,
    114
   ],
   [
    204,
    135
   ],
   [
    222,
    151
   ],
   [
    245,
    158
   ]
  ],
  [
   [
    246,
    113
   ],
   [
    270,
    129
   ],
   [
    292,
    143
   ]
  ],
  [
   [
    310,
    156
   ],
   [
    344,
    157
   ],
   [
    376,
    158
   ],
   [
    407,
    177
   ]
  ],
  [
   [
    411,
    145
   ],
   [
    447,
    145
   ],
   [
    469,
    156
   ]
  ],
  [
   [
    402,
    233
   ],
   [
    432,
    240
   ],
   [
    463,
    247
   ]
  ],
  [
   [
    169,
    49
   ],
   [
    191,
    45
   ],
   [
    213,
    45
   ],
   [
    242,
    50
   ]
  ],
  [
   [
    354,
    54
   ],
   [
    380,
    57
   ],
   [
    410,
    57
   ],
   [
    438,
    68
   ]
  ],
  [
   [
    58,
    98
   ],
   [
    85,
    98
   ],
   [
    106,
    100
   ]
  ]
 ],
 "resources": [
  [
   "steel",
   24,
   182,
   5,
   5,
   7680
  ],
  [
   "copper",
   24,
   189,
   4,
   3,
   1200
  ],
  [
   "coal",
   41,
   189,
   3,
   3,
   700
  ],
  [
   "ironore",
   313,
   72,
   3,
   3,
   12000
  ],
  [
   "copperore",
   464,
   64,
   3,
   3,
   12000
  ],
  [
   "crude",
   495,
   222,
   3,
   3,
   12000
  ],
  [
   "coal",
   510,
   60,
   3,
   3,
   12000
  ],
  [
   "stone",
   516,
   49,
   3,
   3,
   12000
  ],
  [
   "steel",
   169,
   216,
   3,
   3,
   3000
  ],
  [
   "copper",
   305,
   78,
   3,
   3,
   3000
  ],
  [
   "coal",
   457,
   208,
   3,
   3,
   3000
  ]
 ],
 "cores": [
  {
   "id": "core:freight",
   "name": "Occupied freight depot",
   "x": 34,
   "y": 48
  },
  {
   "id": "core:quarry",
   "name": "Occupied quarry works",
   "x": 480,
   "y": 46
  },
  {
   "id": "core:wharf",
   "name": "Occupied waterfront warehouse",
   "x": 499,
   "y": 241
  }
 ],
 "artifacts": [
  {
   "id": "artifact:workshop",
   "name": "Workshop salvage room",
   "x": 162,
   "y": 94
  },
  {
   "id": "artifact:quarry",
   "name": "Quarry equipment cache",
   "x": 465,
   "y": 45
  },
  {
   "id": "artifact:wharf",
   "name": "Secured wharf store",
   "x": 474,
   "y": 243
  }
 ],
 "recruits": [
  [
   "foreman",
   57,
   170
  ],
  [
   "electricians",
   168,
   91
  ],
  [
   "concrete",
   196,
   190
  ],
  [
   "lamplighters",
   419,
   173
  ],
  [
   "surveyors",
   67,
   90
  ],
  [
   "gunsmith",
   285,
   90
  ],
  [
   "railcrew",
   307,
   99
  ]
 ],
 "projects": {
  "station": [
   179,
   198
  ],
  "radio": [
   432,
   180
  ],
  "northStation": [
   269,
   100
  ],
  "workshop": [
   195,
   190
  ],
  "turbine": [
   392,
   238
  ],
  "records": [
   158,
   93
  ],
  "heart": [
   319,
   77
  ],
  "furnace": [
   303,
   63
  ],
  "crown": [
   456,
   91
  ]
 },
 "riverY": 261,
 "court": {
  "x": 37,
  "y": 198,
  "radius": 5.5
 },
 "homeRaidY": 218,
 "squares": [
  {
   "x": 417,
   "y": 156,
   "w": 25,
   "h": 17,
   "name": "Civic Square"
  }
 ],
 "paths": [
  [
   [
    35,
    191
   ],
   [
    35,
    194
   ]
  ],
  [
   [
    58,
    175
   ],
   [
    58,
    178
   ]
  ],
  [
   [
    21,
    211
   ],
   [
    21,
    214
   ]
  ],
  [
   [
    45,
    170
   ],
   [
    45,
    173
   ]
  ],
  [
   [
    50,
    99
   ],
   [
    50,
    102
   ]
  ],
  [
   [
    67,
    95
   ],
   [
    67,
    98
   ]
  ],
  [
   [
    76,
    108
   ],
   [
    76,
    111
   ]
  ],
  [
   [
    42,
    110
   ],
   [
    42,
    113
   ]
  ],
  [
   [
    62,
    78
   ],
   [
    62,
    81
   ]
  ],
  [
   [
    83,
    84
   ],
   [
    83,
    87
   ]
  ],
  [
   [
    72,
    241
   ],
   [
    72,
    244
   ]
  ],
  [
   [
    160,
    175
   ],
   [
    160,
    178
   ]
  ],
  [
   [
    192,
    177
   ],
   [
    192,
    180
   ]
  ],
  [
   [
    203,
    80
   ],
   [
    203,
    83
   ]
  ],
  [
   [
    283,
    206
   ],
   [
    283,
    209
   ]
  ],
  [
   [
    450,
    170
   ],
   [
    450,
    173
   ]
  ],
  [
   [
    474,
    114
   ],
   [
    474,
    117
   ]
  ],
  [
   [
    520,
    189
   ],
   [
    520,
    192
   ]
  ],
  [
   [
    331,
    227
   ],
   [
    331,
    230
   ]
  ],
  [
   [
    168,
    47
   ],
   [
    168,
    50
   ]
  ],
  [
   [
    330,
    71
   ],
   [
    330,
    74
   ]
  ],
  [
   [
    335,
    97
   ],
   [
    335,
    100
   ]
  ],
  [
   [
    508,
    96
   ],
   [
    508,
    99
   ]
  ],
  [
   [
    208,
    182
   ],
   [
    208,
    185
   ]
  ],
  [
   [
    271,
    200
   ],
   [
    271,
    203
   ]
  ],
  [
   [
    265,
    171
   ],
   [
    265,
    174
   ]
  ],
  [
   [
    205,
    113
   ],
   [
    205,
    116
   ]
  ],
  [
   [
    283,
    124
   ],
   [
    283,
    127
   ]
  ],
  [
   [
    271,
    222
   ],
   [
    271,
    225
   ]
  ],
  [
   [
    165,
    99
   ],
   [
    165,
    102
   ]
  ],
  [
   [
    197,
    197
   ],
   [
    197,
    200
   ]
  ],
  [
   [
    171,
    207
   ],
   [
    171,
    210
   ]
  ],
  [
   [
    258,
    86
   ],
   [
    258,
    89
   ]
  ],
  [
   [
    286,
    97
   ],
   [
    286,
    100
   ]
  ],
  [
   [
    308,
    103
   ],
   [
    308,
    106
   ]
  ],
  [
   [
    440,
    199
   ],
   [
    440,
    202
   ]
  ],
  [
   [
    420,
    178
   ],
   [
    420,
    181
   ]
  ],
  [
   [
    37,
    62
   ],
   [
    37,
    65
   ]
  ],
  [
   [
    482,
    61
   ],
   [
    482,
    64
   ]
  ],
  [
   [
    501,
    253
   ],
   [
    501,
    256
   ]
  ],
  [
   [
    465,
    52
   ],
   [
    465,
    55
   ]
  ],
  [
   [
    474,
    249
   ],
   [
    474,
    252
   ]
  ],
  [
   [
    53,
    219
   ],
   [
    53,
    222
   ]
  ],
  [
   [
    19,
    195
   ],
   [
    19,
    198
   ]
  ],
  [
   [
    91,
    206
   ],
   [
    91,
    209
   ]
  ],
  [
   [
    105,
    206
   ],
   [
    105,
    209
   ]
  ],
  [
   [
    120,
    206
   ],
   [
    120,
    209
   ]
  ],
  [
   [
    134,
    206
   ],
   [
    134,
    209
   ]
  ],
  [
   [
    96,
    183
   ],
   [
    96,
    186
   ]
  ],
  [
   [
    113,
    182
   ],
   [
    113,
    185
   ]
  ],
  [
   [
    131,
    183
   ],
   [
    131,
    186
   ]
  ],
  [
   [
    69,
    124
   ],
   [
    69,
    127
   ]
  ],
  [
   [
    85,
    132
   ],
   [
    85,
    135
   ]
  ],
  [
   [
    101,
    133
   ],
   [
    101,
    136
   ]
  ],
  [
   [
    119,
    133
   ],
   [
    119,
    136
   ]
  ],
  [
   [
    136,
    137
   ],
   [
    136,
    140
   ]
  ],
  [
   [
    172,
    145
   ],
   [
    172,
    148
   ]
  ],
  [
   [
    200,
    154
   ],
   [
    200,
    157
   ]
  ],
  [
   [
    217,
    163
   ],
   [
    217,
    166
   ]
  ],
  [
   [
    240,
    150
   ],
   [
    240,
    153
   ]
  ],
  [
   [
    227,
    139
   ],
   [
    227,
    142
   ]
  ],
  [
   [
    279,
    162
   ],
   [
    279,
    165
   ]
  ],
  [
   [
    295,
    177
   ],
   [
    295,
    180
   ]
  ],
  [
   [
    315,
    180
   ],
   [
    315,
    183
   ]
  ],
  [
   [
    339,
    202
   ],
   [
    339,
    205
   ]
  ],
  [
   [
    364,
    206
   ],
   [
    364,
    209
   ]
  ],
  [
   [
    389,
    142
   ],
   [
    389,
    145
   ]
  ],
  [
   [
    433,
    140
   ],
   [
    433,
    143
   ]
  ],
  [
   [
    453,
    140
   ],
   [
    453,
    143
   ]
  ],
  [
   [
    154,
    251
   ],
   [
    154,
    254
   ]
  ],
  [
   [
    178,
    253
   ],
   [
    178,
    256
   ]
  ],
  [
   [
    208,
    252
   ],
   [
    208,
    255
   ]
  ],
  [
   [
    191,
    42
   ],
   [
    191,
    45
   ]
  ],
  [
   [
    219,
    40
   ],
   [
    219,
    43
   ]
  ],
  [
   [
    360,
    46
   ],
   [
    360,
    49
   ]
  ],
  [
   [
    389,
    46
   ],
   [
    389,
    49
   ]
  ],
  [
   [
    416,
    47
   ],
   [
    416,
    50
   ]
  ],
  [
   [
    405,
    216
   ],
   [
    405,
    219
   ]
  ],
  [
   [
    445,
    235
   ],
   [
    445,
    238
   ]
  ],
  [
   [
    455,
    258
   ],
   [
    455,
    261
   ]
  ],
  [
   [
    68,
    49
   ],
   [
    68,
    52
   ]
  ],
  [
   [
    88,
    52
   ],
   [
    88,
    55
   ]
  ],
  [
   [
    305,
    152
   ],
   [
    305,
    155
   ]
  ],
  [
   [
    343,
    147
   ],
   [
    343,
    150
   ]
  ],
  [
   [
    366,
    148
   ],
   [
    366,
    151
   ]
  ],
  [
   [
    391,
    244
   ],
   [
    391,
    247
   ]
  ]
 ]
};
export const RIVERFRONT_BUILDINGS:Parcel[]=[
 {
  "id": "home-workshop",
  "name": "Home workshop",
  "x": 30,
  "y": 177,
  "w": 10,
  "h": 14,
  "kind": "workshop",
  "enterable": true,
  "door": [
   34,
   190
  ],
  "variant": 1
 },
 {
  "id": "home-garage",
  "name": "Salvage garages",
  "x": 16,
  "y": 174,
  "w": 12,
  "h": 7,
  "kind": "garage",
  "variant": 2
 },
 {
  "id": "foreman-shelter",
  "name": "Foreman workshop",
  "x": 53,
  "y": 166,
  "w": 12,
  "h": 9,
  "kind": "workshop",
  "enterable": true,
  "door": [
   57,
   174
  ],
  "content": "foreman",
  "variant": 0
 },
 {
  "id": "court-house",
  "name": "court house",
  "x": 16,
  "y": 202,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   20,
   210
  ],
  "variant": 1
 },
 {
  "id": "court-garage",
  "name": "court garage",
  "x": 40,
  "y": 161,
  "w": 10,
  "h": 9,
  "kind": "garage",
  "enterable": false,
  "door": [
   44,
   169
  ],
  "variant": 2
 },
 {
  "id": "westridge-garden",
  "name": "westridge garden",
  "x": 45,
  "y": 90,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   49,
   98
  ],
  "content": "story",
  "note": "A maintenance ledger marks the electrical salvage workshop east of Westridge. The last crew sheltered there with their tools.",
  "variant": 0
 },
 {
  "id": "westridge-rowan",
  "name": "westridge rowan",
  "x": 62,
  "y": 86,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   66,
   94
  ],
  "content": "surveyors",
  "variant": 1
 },
 {
  "id": "westridge-ash",
  "name": "westridge ash",
  "x": 71,
  "y": 99,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   75,
   107
  ],
  "content": "shortcut",
  "variant": 0
 },
 {
  "id": "westridge-boarded",
  "name": "westridge boarded",
  "x": 37,
  "y": 101,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   41,
   109
  ],
  "variant": 0
 },
 {
  "id": "westridge-upper",
  "name": "westridge upper",
  "x": 57,
  "y": 69,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   61,
   77
  ],
  "variant": 0
 },
 {
  "id": "westridge-corner",
  "name": "westridge corner",
  "x": 78,
  "y": 77,
  "w": 12,
  "h": 7,
  "kind": "house",
  "enterable": false,
  "door": [
   82,
   83
  ],
  "variant": 0
 },
 {
  "id": "canal-corner",
  "name": "canal corner",
  "x": 67,
  "y": 232,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   71,
   240
  ],
  "variant": 2
 },
 {
  "id": "westridge-south",
  "name": "westridge south",
  "x": 155,
  "y": 166,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   159,
   174
  ],
  "variant": 1
 },
 {
  "id": "oldtown-west",
  "name": "oldtown west",
  "x": 187,
  "y": 168,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   191,
   176
  ],
  "variant": 2
 },
 {
  "id": "oldtown-north",
  "name": "oldtown north",
  "x": 198,
  "y": 71,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   202,
   79
  ],
  "variant": 1
 },
 {
  "id": "oldtown-passage",
  "name": "oldtown passage",
  "x": 278,
  "y": 197,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   282,
   205
  ],
  "variant": 0
 },
 {
  "id": "civic-terrace",
  "name": "civic terrace",
  "x": 445,
  "y": 161,
  "w": 10,
  "h": 9,
  "kind": "terrace",
  "enterable": false,
  "door": [
   449,
   169
  ],
  "variant": 2
 },
 {
  "id": "civic-garden",
  "name": "civic garden",
  "x": 469,
  "y": 105,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   473,
   113
  ],
  "variant": 2
 },
 {
  "id": "civic-east",
  "name": "civic east",
  "x": 515,
  "y": 180,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   519,
   188
  ],
  "variant": 1
 },
 {
  "id": "canal-house",
  "name": "canal house",
  "x": 326,
  "y": 218,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   330,
   226
  ],
  "variant": 0
 },
 {
  "id": "north-lodge",
  "name": "north lodge",
  "x": 163,
  "y": 38,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   167,
   46
  ],
  "variant": 1
 },
 {
  "id": "north-corner",
  "name": "north corner",
  "x": 325,
  "y": 62,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   329,
   70
  ],
  "variant": 1
 },
 {
  "id": "north-row",
  "name": "north row",
  "x": 330,
  "y": 88,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   334,
   96
  ],
  "variant": 2
 },
 {
  "id": "east-lodge",
  "name": "east lodge",
  "x": 503,
  "y": 87,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   507,
   95
  ],
  "variant": 1
 },
 {
  "id": "oldtown-orchard",
  "name": "oldtown orchard",
  "x": 203,
  "y": 173,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   207,
   181
  ],
  "content": "story",
  "note": "A route card follows the northbound tram to Ironworks. Freight once kept these streets supplied.",
  "variant": 2
 },
 {
  "id": "oldtown-courtyard",
  "name": "oldtown courtyard",
  "x": 266,
  "y": 191,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   270,
   199
  ],
  "content": "story",
  "note": "The rear alley is choked with light debris. Clearing it opens another route around this court.",
  "variant": 0
 },
 {
  "id": "oldtown-mews",
  "name": "oldtown mews",
  "x": 260,
  "y": 162,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": true,
  "door": [
   264,
   170
  ],
  "content": "story",
  "note": "A notice from the Lamplighters points east, to their workshop beside the civic library.",
  "variant": 1
 },
 {
  "id": "oldtown-row",
  "name": "oldtown row",
  "x": 200,
  "y": 104,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   204,
   112
  ],
  "variant": 0
 },
 {
  "id": "oldtown-east",
  "name": "oldtown east",
  "x": 278,
  "y": 115,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   282,
   123
  ],
  "variant": 1
 },
 {
  "id": "oldtown-south",
  "name": "oldtown south",
  "x": 266,
  "y": 213,
  "w": 10,
  "h": 9,
  "kind": "house",
  "enterable": false,
  "door": [
   270,
   221
  ],
  "variant": 0
 },
 {
  "id": "salvage-workshop",
  "name": "Electrical salvage workshop",
  "x": 155,
  "y": 86,
  "w": 20,
  "h": 13,
  "kind": "workshop",
  "enterable": true,
  "door": [
   164,
   98
  ],
  "content": "records",
  "variant": 0
 },
 {
  "id": "concrete-shelter",
  "name": "Masonry workshop",
  "x": 191,
  "y": 184,
  "w": 13,
  "h": 13,
  "kind": "workshop",
  "enterable": true,
  "door": [
   196,
   196
  ],
  "content": "concrete",
  "variant": 2
 },
 {
  "id": "riverside-pump",
  "name": "Riverside pumping tower",
  "x": 165,
  "y": 191,
  "w": 15,
  "h": 16,
  "kind": "utility",
  "enterable": true,
  "door": [
   170,
   206
  ],
  "content": "plant:riverside",
  "variant": 1
 },
 {
  "id": "ironworks-hall",
  "name": "Ironworks turbine hall",
  "x": 249,
  "y": 65,
  "w": 17,
  "h": 21,
  "kind": "utility",
  "enterable": true,
  "door": [
   257,
   85
  ],
  "content": "plant:ironworks",
  "variant": 1
 },
 {
  "id": "ironworks-gunsmith",
  "name": "Gunsmith machine shop",
  "x": 280,
  "y": 85,
  "w": 13,
  "h": 12,
  "kind": "workshop",
  "enterable": true,
  "door": [
   285,
   96
  ],
  "content": "gunsmith",
  "variant": 1
 },
 {
  "id": "railcrew-depot",
  "name": "Rail maintenance depot",
  "x": 303,
  "y": 92,
  "w": 13,
  "h": 11,
  "kind": "warehouse",
  "enterable": true,
  "door": [
   307,
   102
  ],
  "content": "railcrew",
  "variant": 2
 },
 {
  "id": "civic-utility",
  "name": "Civic Utility",
  "x": 432,
  "y": 181,
  "w": 16,
  "h": 18,
  "kind": "utility",
  "enterable": true,
  "door": [
   439,
   198
  ],
  "content": "plant:civic",
  "variant": 0
 },
 {
  "id": "civic-dome",
  "name": "City library",
  "x": 426,
  "y": 109,
  "w": 17,
  "h": 14,
  "kind": "civic",
  "variant": 2
 },
 {
  "id": "lamplighter-house",
  "name": "Lamplighters workshop",
  "x": 415,
  "y": 168,
  "w": 12,
  "h": 10,
  "kind": "workshop",
  "enterable": true,
  "door": [
   419,
   177
  ],
  "content": "lamplighters",
  "variant": 0
 },
 {
  "id": "freight-stronghold",
  "name": "Northwood freight depot",
  "x": 27,
  "y": 39,
  "w": 30,
  "h": 23,
  "kind": "warehouse",
  "enterable": true,
  "door": [
   36,
   61
  ],
  "content": "core:freight",
  "variant": 1,
  "doors": [
   [
    27,
    49,
    1,
    3
   ]
  ],
  "compound": "core:freight"
 },
 {
  "id": "quarry-stronghold",
  "name": "Ravenholm quarry works",
  "x": 472,
  "y": 38,
  "w": 30,
  "h": 23,
  "kind": "warehouse",
  "enterable": true,
  "door": [
   481,
   60
  ],
  "content": "core:quarry",
  "variant": 1,
  "doors": [
   [
    472,
    48,
    1,
    3
   ]
  ],
  "compound": "core:quarry"
 },
 {
  "id": "wharf-stronghold",
  "name": "Occupied warehouse",
  "x": 491,
  "y": 230,
  "w": 30,
  "h": 23,
  "kind": "warehouse",
  "enterable": true,
  "door": [
   500,
   252
  ],
  "content": "core:wharf",
  "variant": 2,
  "doors": [
   [
    491,
    240,
    1,
    3
   ]
  ],
  "compound": "core:wharf"
 },
 {
  "id": "quarry-cache",
  "name": "Old equipment store",
  "x": 460,
  "y": 39,
  "w": 10,
  "h": 13,
  "kind": "garage",
  "enterable": true,
  "door": [
   464,
   51
  ],
  "content": "artifact:quarry",
  "variant": 0
 },
 {
  "id": "wharf-cache",
  "name": "Customs store",
  "x": 468,
  "y": 234,
  "w": 13,
  "h": 15,
  "kind": "warehouse",
  "enterable": true,
  "door": [
   473,
   248
  ],
  "content": "artifact:wharf",
  "variant": 1
 },
 {
  "id": "north-shed",
  "name": "Ironworks stock shed",
  "x": 279,
  "y": 36,
  "w": 20,
  "h": 11,
  "kind": "warehouse",
  "variant": 0
 },
 {
  "id": "civic-homes",
  "name": "Boarded municipal housing",
  "x": 472,
  "y": 175,
  "w": 17,
  "h": 9,
  "kind": "terrace",
  "variant": 1
 },
 {
  "id": "court-east-home",
  "name": "Court cottage",
  "x": 49,
  "y": 211,
  "w": 9,
  "h": 8,
  "kind": "house",
  "door": [
   52,
   218
  ],
  "variant": 1
 },
 {
  "id": "court-west-garage",
  "name": "Maintenance garage",
  "x": 16,
  "y": 188,
  "w": 7,
  "h": 7,
  "kind": "garage",
  "door": [
   18,
   194
  ],
  "variant": 2
 },
 {
  "id": "row-frontage-0",
  "name": "Detached home",
  "x": 86,
  "y": 197,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   90,
   205
  ],
  "variant": 0
 },
 {
  "id": "row-frontage-1",
  "name": "Detached home",
  "x": 100,
  "y": 197,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   104,
   205
  ],
  "variant": 1
 },
 {
  "id": "row-frontage-2",
  "name": "Detached home",
  "x": 115,
  "y": 197,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   119,
   205
  ],
  "variant": 2
 },
 {
  "id": "row-frontage-3",
  "name": "Detached home",
  "x": 129,
  "y": 197,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   133,
   205
  ],
  "variant": 0
 },
 {
  "id": "row-frontage-4",
  "name": "Detached home",
  "x": 91,
  "y": 174,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   95,
   182
  ],
  "variant": 1
 },
 {
  "id": "row-frontage-5",
  "name": "Detached home",
  "x": 108,
  "y": 173,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   112,
   181
  ],
  "variant": 2
 },
 {
  "id": "row-frontage-6",
  "name": "Detached home",
  "x": 126,
  "y": 174,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   130,
   182
  ],
  "variant": 0
 },
 {
  "id": "westridge-frontage-0",
  "name": "Detached home",
  "x": 64,
  "y": 115,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   68,
   123
  ],
  "variant": 0
 },
 {
  "id": "westridge-frontage-1",
  "name": "Detached home",
  "x": 80,
  "y": 123,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   84,
   131
  ],
  "variant": 1
 },
 {
  "id": "westridge-frontage-2",
  "name": "Detached home",
  "x": 96,
  "y": 124,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   100,
   132
  ],
  "variant": 2
 },
 {
  "id": "westridge-frontage-3",
  "name": "Detached home",
  "x": 114,
  "y": 124,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   118,
   132
  ],
  "variant": 0
 },
 {
  "id": "westridge-frontage-4",
  "name": "Detached home",
  "x": 131,
  "y": 128,
  "w": 10,
  "h": 9,
  "kind": "house",
  "door": [
   135,
   136
  ],
  "variant": 1
 },
 {
  "id": "market-frontage-0",
  "name": "Neighbourhood shop",
  "x": 166,
  "y": 136,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   171,
   144
  ],
  "variant": 0
 },
 {
  "id": "market-frontage-1",
  "name": "Neighbourhood shop",
  "x": 194,
  "y": 145,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   199,
   153
  ],
  "variant": 1
 },
 {
  "id": "market-frontage-2",
  "name": "Neighbourhood shop",
  "x": 211,
  "y": 154,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   216,
   162
  ],
  "variant": 2
 },
 {
  "id": "market-frontage-3",
  "name": "Neighbourhood shop",
  "x": 234,
  "y": 141,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   239,
   149
  ],
  "variant": 0
 },
 {
  "id": "oldtown-frontage-0",
  "name": "Terrace homes",
  "x": 218,
  "y": 129,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   226,
   138
  ],
  "variant": 0
 },
 {
  "id": "oldtown-frontage-1",
  "name": "Terrace homes",
  "x": 270,
  "y": 152,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   278,
   161
  ],
  "variant": 1
 },
 {
  "id": "oldtown-frontage-2",
  "name": "Terrace homes",
  "x": 286,
  "y": 167,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   294,
   176
  ],
  "variant": 2
 },
 {
  "id": "oldtown-frontage-3",
  "name": "Terrace homes",
  "x": 306,
  "y": 170,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   314,
   179
  ],
  "variant": 0
 },
 {
  "id": "oldtown-frontage-4",
  "name": "Terrace homes",
  "x": 330,
  "y": 192,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   338,
   201
  ],
  "variant": 1
 },
 {
  "id": "oldtown-frontage-5",
  "name": "Terrace homes",
  "x": 355,
  "y": 196,
  "w": 18,
  "h": 10,
  "kind": "terrace",
  "door": [
   363,
   205
  ],
  "variant": 2
 },
 {
  "id": "civic-frontage-0",
  "name": "Neighbourhood shop",
  "x": 383,
  "y": 133,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   388,
   141
  ],
  "variant": 0
 },
 {
  "id": "civic-frontage-1",
  "name": "Neighbourhood shop",
  "x": 427,
  "y": 131,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   432,
   139
  ],
  "variant": 1
 },
 {
  "id": "civic-frontage-2",
  "name": "Neighbourhood shop",
  "x": 447,
  "y": 131,
  "w": 12,
  "h": 9,
  "kind": "shop",
  "door": [
   452,
   139
  ],
  "variant": 2
 },
 {
  "id": "riverside-frontage-0",
  "name": "Waterside workshop",
  "x": 146,
  "y": 240,
  "w": 16,
  "h": 11,
  "kind": "workshop",
  "door": [
   153,
   250
  ],
  "variant": 0
 },
 {
  "id": "riverside-frontage-1",
  "name": "Waterside workshop",
  "x": 170,
  "y": 242,
  "w": 16,
  "h": 11,
  "kind": "workshop",
  "door": [
   177,
   252
  ],
  "variant": 1
 },
 {
  "id": "riverside-frontage-2",
  "name": "Waterside workshop",
  "x": 200,
  "y": 241,
  "w": 16,
  "h": 11,
  "kind": "workshop",
  "door": [
   207,
   251
  ],
  "variant": 2
 },
 {
  "id": "iron-frontage-0",
  "name": "Freight warehouse",
  "x": 181,
  "y": 28,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   190,
   41
  ],
  "variant": 0
 },
 {
  "id": "iron-frontage-1",
  "name": "Freight warehouse",
  "x": 209,
  "y": 26,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   218,
   39
  ],
  "variant": 1
 },
 {
  "id": "iron-frontage-2",
  "name": "Freight warehouse",
  "x": 350,
  "y": 32,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   359,
   45
  ],
  "variant": 2
 },
 {
  "id": "iron-frontage-3",
  "name": "Freight warehouse",
  "x": 379,
  "y": 32,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   388,
   45
  ],
  "variant": 0
 },
 {
  "id": "iron-frontage-4",
  "name": "Freight warehouse",
  "x": 406,
  "y": 33,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   415,
   46
  ],
  "variant": 1
 },
 {
  "id": "dock-frontage-0",
  "name": "Freight warehouse",
  "x": 395,
  "y": 202,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   404,
   215
  ],
  "variant": 0
 },
 {
  "id": "dock-frontage-1",
  "name": "Freight warehouse",
  "x": 435,
  "y": 221,
  "w": 20,
  "h": 14,
  "kind": "warehouse",
  "door": [
   444,
   234
  ],
  "variant": 1
 },
 {
  "id": "dock-frontage-2",
  "name": "Freight warehouse",
  "x": 445,
  "y": 248,
  "w": 20,
  "h": 10,
  "kind": "warehouse",
  "door": [
   454,
   257
  ],
  "variant": 2
 },
 {
  "id": "freight-frontage-0",
  "name": "Service garage",
  "x": 63,
  "y": 42,
  "w": 10,
  "h": 7,
  "kind": "garage",
  "door": [
   67,
   48
  ],
  "variant": 0
 },
 {
  "id": "freight-frontage-1",
  "name": "Service garage",
  "x": 83,
  "y": 45,
  "w": 10,
  "h": 7,
  "kind": "garage",
  "door": [
   87,
   51
  ],
  "variant": 1
 },
 {
  "id": "mid-frontage-0",
  "name": "Service garage",
  "x": 300,
  "y": 145,
  "w": 10,
  "h": 7,
  "kind": "garage",
  "door": [
   304,
   151
  ],
  "variant": 0
 },
 {
  "id": "mid-frontage-1",
  "name": "Service garage",
  "x": 338,
  "y": 140,
  "w": 10,
  "h": 7,
  "kind": "garage",
  "door": [
   342,
   146
  ],
  "variant": 1
 },
 {
  "id": "mid-frontage-2",
  "name": "Service garage",
  "x": 361,
  "y": 141,
  "w": 10,
  "h": 7,
  "kind": "garage",
  "door": [
   365,
   147
  ],
  "variant": 2
 },
 {
  "id": "turbine-hall",
  "name": "Waterside turbine hall",
  "x": 386,
  "y": 228,
  "w": 14,
  "h": 16,
  "kind": "utility",
  "enterable": true,
  "door": [
   390,
   243
  ],
  "variant": 1,
  "content": "turbine"
 }
];
export const RIVERFRONT_PROPS:MapProp[]=[
 {
  "id": "riverside-tank-west",
  "x": 181,
  "y": 191,
  "w": 3,
  "h": 3,
  "kind": "tank"
 },
 {
  "id": "riverside-tank-east",
  "x": 185,
  "y": 191,
  "w": 3,
  "h": 3,
  "kind": "tank"
 },
 {
  "id": "wharf-crane",
  "x": 487,
  "y": 256,
  "w": 3,
  "h": 3,
  "kind": "crane"
 },
 {
  "id": "tree:0",
  "x": 21,
  "y": 165,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:1",
  "x": 27,
  "y": 167,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:2",
  "x": 46,
  "y": 173,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:3",
  "x": 50,
  "y": 176,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:4",
  "x": 70,
  "y": 175,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:5",
  "x": 84,
  "y": 202,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:6",
  "x": 82,
  "y": 228,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:7",
  "x": 61,
  "y": 215,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:8",
  "x": 35,
  "y": 161,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:9",
  "x": 32,
  "y": 85,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:10",
  "x": 48,
  "y": 80,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:11",
  "x": 61,
  "y": 82,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:12",
  "x": 76,
  "y": 92,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:13",
  "x": 83,
  "y": 108,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:14",
  "x": 186,
  "y": 107,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:15",
  "x": 200,
  "y": 166,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:16",
  "x": 245,
  "y": 183,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:17",
  "x": 283,
  "y": 187,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:18",
  "x": 279,
  "y": 216,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:19",
  "x": 317,
  "y": 201,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:20",
  "x": 327,
  "y": 179,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:21",
  "x": 406,
  "y": 106,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:22",
  "x": 419,
  "y": 96,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:23",
  "x": 450,
  "y": 172,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:24",
  "x": 463,
  "y": 177,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:25",
  "x": 481,
  "y": 211,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:26",
  "x": 503,
  "y": 213,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:27",
  "x": 511,
  "y": 222,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:28",
  "x": 447,
  "y": 241,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:29",
  "x": 414,
  "y": 250,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:30",
  "x": 271,
  "y": 255,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:31",
  "x": 192,
  "y": 248,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:32",
  "x": 71,
  "y": 246,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:33",
  "x": 51,
  "y": 241,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:34",
  "x": 15,
  "y": 240,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:35",
  "x": 23,
  "y": 70,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:36",
  "x": 69,
  "y": 59,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:37",
  "x": 321,
  "y": 41,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:38",
  "x": 436,
  "y": 61,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "tree:39",
  "x": 493,
  "y": 69,
  "w": 2,
  "h": 2,
  "kind": "tree"
 },
 {
  "id": "court-brush",
  "x": 72,
  "y": 208,
  "w": 2,
  "h": 2,
  "kind": "debris",
  "clearable": true
 },
 {
  "id": "oldtown-alley",
  "x": 262,
  "y": 197,
  "w": 3,
  "h": 2,
  "kind": "debris",
  "clearable": true
 },
 {
  "id": "westridge-gate",
  "x": 80,
  "y": 104,
  "w": 3,
  "h": 3,
  "kind": "gate"
 },
 {
  "id": "alley-fence-north",
  "x": 81,
  "y": 99,
  "w": 2,
  "h": 5,
  "kind": "fence"
 },
 {
  "id": "alley-fence-south",
  "x": 81,
  "y": 107,
  "w": 2,
  "h": 9,
  "kind": "fence"
 },
 {
  "id": "westridge-garden:table",
  "x": 47,
  "y": 92,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "westridge-garden:bed",
  "x": 52,
  "y": 95,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "westridge-rowan:table",
  "x": 64,
  "y": 88,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "westridge-rowan:bed",
  "x": 69,
  "y": 91,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "westridge-ash:table",
  "x": 73,
  "y": 101,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "westridge-ash:bed",
  "x": 72,
  "y": 104,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "oldtown-orchard:table",
  "x": 205,
  "y": 175,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "oldtown-orchard:bed",
  "x": 210,
  "y": 178,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "oldtown-courtyard:table",
  "x": 268,
  "y": 193,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "oldtown-courtyard:bed",
  "x": 273,
  "y": 196,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "oldtown-mews:table",
  "x": 262,
  "y": 164,
  "w": 2,
  "h": 2,
  "kind": "furniture"
 },
 {
  "id": "oldtown-mews:bed",
  "x": 267,
  "y": 167,
  "w": 2,
  "h": 3,
  "kind": "furniture"
 },
 {
  "id": "freight-stronghold:prop0",
  "x": 30,
  "y": 42,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "freight-stronghold:prop1",
  "x": 47,
  "y": 42,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "freight-stronghold:prop2",
  "x": 49,
  "y": 53,
  "w": 3,
  "h": 3,
  "kind": "converter"
 },
 {
  "id": "freight-stronghold:prop3",
  "x": 30,
  "y": 55,
  "w": 4,
  "h": 2,
  "kind": "debris"
 },
 {
  "id": "quarry-stronghold:prop0",
  "x": 475,
  "y": 41,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "quarry-stronghold:prop1",
  "x": 492,
  "y": 41,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "quarry-stronghold:prop2",
  "x": 494,
  "y": 52,
  "w": 3,
  "h": 3,
  "kind": "converter"
 },
 {
  "id": "quarry-stronghold:prop3",
  "x": 475,
  "y": 54,
  "w": 4,
  "h": 2,
  "kind": "debris"
 },
 {
  "id": "wharf-stronghold:prop0",
  "x": 494,
  "y": 233,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "wharf-stronghold:prop1",
  "x": 511,
  "y": 233,
  "w": 6,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "wharf-stronghold:prop2",
  "x": 513,
  "y": 244,
  "w": 3,
  "h": 3,
  "kind": "converter"
 },
 {
  "id": "wharf-stronghold:prop3",
  "x": 494,
  "y": 246,
  "w": 4,
  "h": 2,
  "kind": "debris"
 },
 {
  "id": "freight-cargo-a",
  "x": 15,
  "y": 72,
  "w": 9,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "freight-cargo-b",
  "x": 48,
  "y": 72,
  "w": 8,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "freight-loader",
  "x": 61,
  "y": 39,
  "w": 6,
  "h": 4,
  "kind": "excavator"
 },
 {
  "id": "quarry-processor",
  "x": 503,
  "y": 67,
  "w": 9,
  "h": 4,
  "kind": "container"
 },
 {
  "id": "quarry-excavator",
  "x": 478,
  "y": 65,
  "w": 7,
  "h": 4,
  "kind": "excavator"
 },
 {
  "id": "wharf-cargo-a",
  "x": 491,
  "y": 215,
  "w": 9,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "wharf-cargo-b",
  "x": 506,
  "y": 215,
  "w": 9,
  "h": 3,
  "kind": "container"
 },
 {
  "id": "quarry-boulder",
  "x": 464,
  "y": 28,
  "w": 4,
  "h": 3,
  "kind": "rock"
 },
 {
  "id": "freight-stronghold:partition0",
  "x": 43,
  "y": 41,
  "w": 1,
  "h": 7,
  "kind": "fence"
 },
 {
  "id": "freight-stronghold:partition1",
  "x": 43,
  "y": 53,
  "w": 1,
  "h": 7,
  "kind": "fence"
 },
 {
  "id": "quarry-stronghold:partition0",
  "x": 488,
  "y": 40,
  "w": 1,
  "h": 7,
  "kind": "fence"
 },
 {
  "id": "quarry-stronghold:partition1",
  "x": 488,
  "y": 52,
  "w": 1,
  "h": 7,
  "kind": "fence"
 },
 {
  "id": "wharf-stronghold:partition0",
  "x": 507,
  "y": 232,
  "w": 1,
  "h": 7,
  "kind": "fence"
 },
 {
  "id": "wharf-stronghold:partition1",
  "x": 507,
  "y": 244,
  "w": 1,
  "h": 7,
  "kind": "fence"
 }
];
export function lineTiles(points:readonly [number,number][]):number[]{const out:number[]=[];for(let i=1;i<points.length;i++){let [x,y]=points[i-1];const [gx,gy]=points[i],dx=Math.abs(gx-x),dy=Math.abs(gy-y),sx=Math.sign(gx-x),sy=Math.sign(gy-y);let error=dx-dy;while(x!==gx||y!==gy){const t=y*RIVERFRONT.width+x;if(out.at(-1)!==t)out.push(t);const e=error*2;if(e>-dy){error-=dy;x+=sx;}else{error+=dx;y+=sy;}}}const [x,y]=points.at(-1)!;out.push(y*RIVERFRONT.width+x);return out;}
let cached:CityGeom|undefined;
export function riverfrontGeom():CityGeom{
 if(cached)return cached;const tw=RIVERFRONT.width,th=RIVERFRONT.height,n=tw*th,kind=new Uint8Array(n),owner=new Int32Array(n),near=new Int32Array(n),tiles=RIVERFRONT.regions.map(()=>[] as number[]);
 for(let y=0;y<th;y++)for(let x=0;x<tw;x++){const t=y*tw+x,bank=RIVERFRONT.riverY+Math.round(6*Math.sin(x/52));if(y>=bank){kind[t]=2;owner[t]=-2;near[t]=-1;continue;}let bi=0,d=Infinity;RIVERFRONT.regions.forEach(([,cx,cy],i)=>{const dd=(x-cx)**2+(y-cy)**2;if(dd<d){d=dd;bi=i;}});owner[t]=near[t]=bi;}
 RIVERFRONT.yards.forEach((p,i)=>{for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)owner[y*tw+x]=near[y*tw+x]=i;});
 for(const path of [...RIVERFRONT.roads,RIVERFRONT.tram])for(const t of lineTiles(path))for(let dy=-3;dy<=3;dy++)for(let dx=-3;dx<=3;dx++){const x=t%tw+dx,y=Math.floor(t/tw)+dy;if(x<0||y<0||x>=tw||y>=th||dx*dx+dy*dy>10)continue;const q=y*tw+x;if(kind[q]!==2){kind[q]=1;owner[q]=-1;}}
 for(let y=192;y<=204;y++)for(let x=31;x<=43;x++)if(Math.hypot(x-RIVERFRONT.court.x,y-RIVERFRONT.court.y)<=RIVERFRONT.court.radius){kind[y*tw+x]=1;owner[y*tw+x]=-1;}
 // Reserved yards and building parcels override roads, with authored doors and drives connecting them.
 for(const p of [...RIVERFRONT.yards,...RIVERFRONT_BUILDINGS])for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++){const t=y*tw+x;kind[t]=0;owner[t]=near[t];}
 for(const path of RIVERFRONT.paths)for(const t of lineTiles(path))if(kind[t]!==2){kind[t]=1;owner[t]=-1;}
 for(let t=0;t<n;t++)if(owner[t]>=0)tiles[owner[t]].push(t);
 const blocks:CityBlock[]=RIVERFRONT.regions.map(([,cx,cy],id)=>{const ts=tiles[id],xs=ts.map(t=>t%tw),ys=ts.map(t=>Math.floor(t/tw));return{id,cx,cy,tiles:Int32Array.from(ts),area:ts.length,x0:Math.min(...xs),x1:Math.max(...xs),y0:Math.min(...ys),y1:Math.max(...ys),sq:24,sqx:id===0?23:cx-12,sqy:id===0?175:cy-12,nb:RIVERFRONT.regions.map((_,i)=>i).filter(i=>i!==id),river:id===0||id===1||id===8,edge:false,inert:false};});
 const segs:CityGeom['segs']=[],segAt=new Map<number,number>();for(let a=0;a<blocks.length;a++)for(let b=a+1;b<blocks.length;b++){segAt.set(a*blocks.length+b,segs.length);const ridge=Int32Array.from(lineTiles([[blocks[a].cx,blocks[a].cy],[blocks[b].cx,blocks[b].cy]]));segs.push({a,b,len:ridge.length,ridge,frontA:new Int32Array(),frontB:new Int32Array(),mx:blocks[a].cx,my:blocks[a].cy});}
 return cached={seed:0,preset:RIVERFRONT_ID,attempt:0,tw,th,kind,owner,near,blocks,segs,segAt,hops:Int32Array.from(blocks.map((_,i)=>i?1:0)),hq:0,foundry:1,wells:[],facilities:[],survivors:[],district:Uint8Array.from([1,2,2,0,1,1,2,3,2]),valid:true,reasons:[]};
}
export function riverfrontSpec():MapSpec{const g=riverfrontGeom();return {w:g.tw,h:g.th,start:[43,197],target:[185,215],wells:[],cells:g.blocks.map(b=>({x:b.cx,y:b.cy,name:'ind',well:false,dmax:.5,g:0,d0:.1,base:.5,gBase:0})),scatteredInert:[],facilities:[],survivors:[],graph:{nb:g.blocks.map(b=>b.nb),len:g.blocks.map(b=>b.nb.map(j=>Math.hypot(b.cx-g.blocks[j].cx,b.cy-g.blocks[j].cy))),area:g.blocks.map(b=>b.area),inert:[],pitch:1},city:{seed:0,preset:RIVERFRONT_ID,tw:g.tw,th:g.th,mapId:RIVERFRONT_ID}};}

export type Lumberjack = {
  "version": "0.1.0",
  "name": "lumberjack",
  "instructions": [
    {
      "name": "initPlayer",
      "accounts": [
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        },
        {
          "name": "systemProgram",
          "isMut": false,
          "isSigner": false
        }
      ],
      "args": []
    },
    {
      "name": "chopTree",
      "accounts": [
        {
          "name": "sessionToken",
          "isMut": false,
          "isSigner": false,
          "isOptional": true
        },
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        }
      ],
      "args": []
    },
    {
      "name": "update",
      "accounts": [
        {
          "name": "sessionToken",
          "isMut": false,
          "isSigner": false,
          "isOptional": true
        },
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        }
      ],
      "args": []
    }
  ],
  "accounts": [
    {
      "name": "playerData",
      "type": {
        "kind": "struct",
        "fields": [
          {
            "name": "authority",
            "type": "publicKey"
          },
          {
            "name": "name",
            "type": "string"
          },
          {
            "name": "level",
            "type": "u8"
          },
          {
            "name": "xp",
            "type": "u64"
          },
          {
            "name": "wood",
            "type": "u64"
          },
          {
            "name": "energy",
            "type": "u64"
          },
          {
            "name": "lastLogin",
            "type": "i64"
          }
        ]
      }
    }
  ],
  "errors": [
    {
      "code": 6000,
      "name": "NotEnoughEnergy",
      "msg": "Not enough energy"
    },
    {
      "code": 6001,
      "name": "WrongAuthority",
      "msg": "Wrong Authority"
    }
  ]
};

export const IDL: Lumberjack = {
  "version": "0.1.0",
  "name": "lumberjack",
  "instructions": [
    {
      "name": "initPlayer",
      "accounts": [
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        },
        {
          "name": "systemProgram",
          "isMut": false,
          "isSigner": false
        }
      ],
      "args": []
    },
    {
      "name": "chopTree",
      "accounts": [
        {
          "name": "sessionToken",
          "isMut": false,
          "isSigner": false,
          "isOptional": true
        },
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        }
      ],
      "args": []
    },
    {
      "name": "update",
      "accounts": [
        {
          "name": "sessionToken",
          "isMut": false,
          "isSigner": false,
          "isOptional": true
        },
        {
          "name": "player",
          "isMut": true,
          "isSigner": false
        },
        {
          "name": "signer",
          "isMut": true,
          "isSigner": true
        }
      ],
      "args": []
    }
  ],
  "accounts": [
    {
      "name": "playerData",
      "type": {
        "kind": "struct",
        "fields": [
          {
            "name": "authority",
            "type": "publicKey"
          },
          {
            "name": "name",
            "type": "string"
          },
          {
            "name": "level",
            "type": "u8"
          },
          {
            "name": "xp",
            "type": "u64"
          },
          {
            "name": "wood",
            "type": "u64"
          },
          {
            "name": "energy",
            "type": "u64"
          },
          {
            "name": "lastLogin",
            "type": "i64"
          }
        ]
      }
    }
  ],
  "errors": [
    {
      "code": 6000,
      "name": "NotEnoughEnergy",
      "msg": "Not enough energy"
    },
    {
      "code": 6001,
      "name": "WrongAuthority",
      "msg": "Wrong Authority"
    }
  ]
};

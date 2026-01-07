# AWS Lambda Empty Function Project

This starter project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

You may also have a test project depending on the options selected.

The generated function handler is a simple method accepting a string argument that returns the uppercase equivalent of the input string. Replace the body of this method, and parameters, to suit your needs. 

## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "Sub2SubApi/test/Sub2SubApi.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "Sub2SubApi/src/Sub2SubApi"
    dotnet lambda deploy-function
```


Dynamo DB structure
```
Item Shape #1 — Lobby metadata (1 item per lobby)

Keys
PK = LOBBY#test-lobby
SK = META

Attributes
{
  "PK": "LOBBY#test-lobby",
  "SK": "META",
  "TournamentName": "Bronze War",
  "StartsAtIso": "2026-01-06T21:00:00Z",
  "Status": "OPEN"
}


Item Shape #2 — Top bid per team + role 

Keys
PK = LOBBY#test-lobby
SK = TOPBID#0#TOP

Attributes
{
  "PK": "LOBBY#test-lobby",
  "SK": "TOPBID#0#TOP",
  "TopBidCredits": 120,
  "TopBidderUserId": "user-123",
  "UpdatedAtEpoch": 1736205600
}


Item Shape #3 — (Future) Role assignment / winner

Keys
PK = LOBBY#test-lobby
SK = ASSIGN#0#TOP

Attributes
{
  "PK": "LOBBY#test-lobby",
  "SK": "ASSIGN#0#TOP",
  "UserId": "user-123",
  "DisplayName": "OptimalLulz",
  "AvatarUrl": "https://...",
  "MatchCode": "ABCD-1234"
}

```
# Portkey DID Verifier

 BRANCH | AZURE PIPELINES                                                                                                                                                                                                                                                  | TESTS                                                                                                                                                                                                                                                   | CODE COVERAGE                                                                                                                                                                           
--------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 MASTER | [![Build Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-DID-verifier?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=11&branchName=master) | [![Test Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-DID-server?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=11&branchName=master) | [![codecov](https://codecov.io/gh/Portkey-Wallet/portkey-DID-verifier/branch/master/graph/badge.svg?token=CZMZ5NGDDW)](https://codecov.io/gh/Portkey-Wallet/portkey-DID-verifier) 


The project is used to undertake third-party services and verify the legitimacy of CAServer users.The project is developed based on the ABP framework, using the Orleans framework, with Grain as the application's business logic implementation and abstraction, and Silo as the host service.Implementation includes verification code services, third-party login (Google, Apple account), and other functions.

## Installation

Before cloning the code and running the project, you need to install the following dependencies:

1. Dotnet7.0+
2. Mongodb
3. Redis
4. Nginx

### Git Clone

The following command will clone Portkey DID Verifier Server code into a folder. Please open a terminal and enter the
following command:

```Bash
git clone https://github.com/Portkey-Wallet/portkey-DID-verifier
```

The next step is to build the project to ensure everything is working correctly. Once everything is built, you can run
as follows:

```Bash
# enter the Launcher folder and publish 

dotnet publish


```

you should startup your mongodb and redis server before you run the project.And then you should first startup the Silo
Module and then startup the HttpApi.Host Module.

### Test

You can easily run unit tests on the project using the following command under the Launcher folder:

```Bash
dotnet test
```

## Usage

The Portkey Contract provides the following modules:

- `CAVerifierServer.Application`: Business logic processing module.
- `CAVerifierServer.Application.Contracts`: Interface definition module.
- `CAVerifierServer.AuthServer`: Authentication Service Module.
- `CAVerifierServer.DbMigrator`: Data initialization module
- `CAVerifierServer.Grains`: Grain processing data module
- `CAVerifierServer.EntityEventHandler`: Event driven module.
- `CAVerifierServer.HttpApi`: HttpApi module.

After starting the service, you can access Swagger at the following address: http://localhost:5577/swagger/index.html
Due to the use of a whitelist mechanism in the project, you need to create a whitelist in Nginx's' X-Forwarded For '. If you are using a local environment, you can directly
configure your IP address to the whitelist in order to access the interface, or comment out the whitelist interception middleware configuration in the startup class. This way, you can access Swagger and access the interface normally.


## Contributing

We welcome contributions to the Portkey DID Verifier Server  project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested and adheres to the aelf coding standards.

## License

Portkey DID Verifier Server is licensed under [MIT](https://github.com/Portkey-Wallet/portkey-DID-verifier/blob/master/LICENSE).


## Contact

If you have any questions or feedback, please feel free to contact us at the Portkey community channels. You can find us on Discord, Telegram, and other social media platforms.

Links:

- Website: https://portkey.finance/
- Twitter: https://twitter.com/Portkey_DID
- Discord: https://discord.com/invite/EUBq3rHQhr

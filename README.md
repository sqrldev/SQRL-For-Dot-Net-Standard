![alt text](https://www.grc.com/sqrl/SQRL_Logo_80x80.png "SQRL Logo")
# SQRL For .Net Standard
SQRL (Secure Quick Reliable Login) for the .Net Standard runtimes.

## Secure Quick Reliable Login
Is a highly secure user privacy based authentication system that removes the need for users to have more than one password for a global identity https://www.grc.com/sqrl/sqrl.htm for more information of the protocal.

## How to install
You can install this as a package by running the following:
Package manager
```
Install-Package SqrlForNet
```

CLI
```
dotnet add package SqrlForNet
```

Or searching for it in with Nuget Manager within your project

## Requirements
To use this package you will need a .Net Standard 2.0 app

For use with ASP.net you will need a .Net core 2.2 or later version

## How to use
Once you have installed the package you can stard using the middleware with a little bit of setup.

In the StartUp.cs file (unless you moved this to another place)
Add this line to the ConfigureServices method
``` csharp
services.AddAuthentication()
  .AddSqrl(options =>
  {
    options.UserExists = UserExists;
    options.UpdateUserId = UpdateUserId;
    options.GetUserVuk = GetUserVuk;
    options.UnlockUser = UnlockUser;
  });
```
You will also need to make sure you have this in the Configure method
``` csharp
app.UseAuthentication();
```

You will probably of noticed that the AddSqrl has options these are explained in the wiki.

## WIP
This package is a work in progress but is working for a simple login and out the SQRL protocal requires some exstra stuff to be done to be fully compleate but this will not take long.

## Warning
It is the publishers beleafe that this code is secure and crypto correct but I am awaiting confermation on this.

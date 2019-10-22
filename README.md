<img src='https://www.grc.com/sqrl/SQRL_Logo_80x80.png' alt='QR Code' height="64"><img src='https://chart.googleapis.com/chart?cht=qr&chl=https%3A%2F%2Fgithub.com%2FTechLiam%2FSQRL-For-Dot-Net-Standard&chs=180x180&choe=UTF-8&chld=L|2' alt='QR Code' height="64"><img src='https://cdn.brandfolder.io/5H442O3W/as/pl546j-7le8zk-5guop3/Slack_RGB.png' alt='Slack logo' height="64">

# SQRL For .Net Standard
SQRL (Secure Quick Reliable Login) for the .Net Standard runtimes.

## Secure Quick Reliable Login
Is a highly secure user privacy based authentication system that removes the need for users to have more than one password for a global identity https://www.grc.com/sqrl/sqrl.htm for more information of the protocol.

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
    options.LockUser = LockUser;
    options.GetUserSuk = GetUserSuk;
    options.RemoveUser = RemoveUser;
  });
```
You will also need to make sure you have this in the Configure method
``` csharp
app.UseAuthentication();
```

You will probably of noticed that the AddSqrl has options these are explained in the wiki.

## Contact for help
Slack: https://sqrlfornet.slack.com/

Join the workspace here: https://join.slack.com/t/sqrlfornet/shared_invite/enQtNzkwNTcxNDM2MjMwLTIxODYwZDZlMTJkNGFkMjBlY2ZjNDBlMDA0ZjExYjA1ZTQ2ZGRmYjY3MzdlZDlmY2U0NTdlNjFlMGI0OWNiOWQ

## Road map
| Version | Goals | Status |
| ------- | ----- | ------ |
| 0.1.0   | Simple login | Released |
| 0.5.0   | lock/unlock/remove SQRL users | Released |
| 0.6.0   | Diagnostics page for debugging | Released |
| 0.8.0   | Custom login pages override middleware | Released |
| 0.9.0   | Examples of NUT storage and management | Released |
| 1.0.0   | Add options for SqrlOnly and Hardlock | Released |
| 1.1.0   | Add Ask capability | Released |
| 1.2.0   | <ul><li>Add async hooks for cleaner database operations</li><li>Add logging</li></ul> | Released |
| 1.3.0   | Change to how hooks work and added HTTPContext access to all hooks | Released |
| 1.4.0   | Imporve the QR code for high resolution devices | Developing |

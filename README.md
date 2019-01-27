------------------------------------------------------

**RockBLOCK9603ConsoleApp.exe**

Type: C# Console Application 

Description: A command-line interface for the 
RockBLOCK 9603 Module.

Parameter0:string = COM Port Name

------------------------------------------------------


------------------------------------------------------

**Start & Shutdown Instructions**

Start

1.) Using CMD.exe, type the path and then press enter. 

e.g. "cd C:\\...\Debug"

2.) Type the application name and argument and then press enter. 

e.g. "RockBLOCK9603ConsoleApp.exe COM5"

Shutdown

1.) If a error occurs, the application will shutdown
by itself. Otherwise, type "exit" and then press enter.

------------------------------------------------------


------------------------------------------------------

**Special Notes**

1.) Enabling "A/" is unsupported and will be ignored. 

2.) Enabling "E0" and "Q1", at the same time, is unsupported
and will throw a TimeoutException. 

3.) Enabling "V0" is unsupported and will throw a TimeoutException. 

4.) The result from "%R" is truncated to 340 characters.  

------------------------------------------------------


------------------------------------------------------

**Contact** 

Developer: Danny Serrano

Email: danyymx9@gmail.com

------------------------------------------------------

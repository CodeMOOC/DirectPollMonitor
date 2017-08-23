# Monitor for DirectPoll.com polls

This simple project monitors a poll opened on [DirectPoll](http://directpoll.com/) through a Web Socket connection and detects new votes as they come in.
New votes can generate actions on the monitor, for instance generating keyboard events.

This sample has been used in conjunction with a [Scratch](https://scratch.mit.edu) project.
The Scratch program, running inside a browser, could be manipulated by a crowd voting on a DirectPoll poll.
Each detected vote would generate a keyboard event that, once forwarded to the Scratch program through the browser, could generate a visible action on screen.

Developed for the [Coding in Your Classroom, Now!](http://platform.europeanmoocs.eu/course_coding_in_your_classroom_now) MOOC initiative.

## How to

Requires .NET 4.5 to run, runs as command-line application.

Specify **voting** address given by DirectPoll:

```
DirectPollMonitor.exe http://directpoll.com/r?XYZ123XYZ123
```

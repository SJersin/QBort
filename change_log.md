# QBort's Report Card (!)

----

## Version 0.10.2

### New Commands

#### Swap

Replaces a player in the current group with a specifc player.
> `swap @jersin @sum1btr`

will remove jersin and replace them with sum1btr.

#### Replace

*Finally (re-)implemented!!*
Calls a new player to replace one that is unable to participate after they have been called.
> Ex: `replace @Johnny @May`

will remove Johnny and May from the group and replace them each with another 'fairandomly' pulled player.

### Old Commands

#### Formatting

Replaced most of the bot's plain message responses with new [^1] formatted embeds.  ✨

The list format for pulled players can be now be set by using the `set-listf` command with the format number you would like to change to.

Current available formats:

- 0 - Plain
  - A simple, horizontal, comma seperated group list.
- 1 - Single Column
  - A simple, vertical group list.
- 2 - Double Column
  - QBort's original (tm) team-like two-column split list.

> Ex: `set-listf 1`

will have the bot display all users pulled by the `new` command in a single column embed list.

## Version 0.10.1

Refactored most commands.
Refactored Handlers for better handling. [^2]

[^1]: To me.
[^2]: This is subjective.

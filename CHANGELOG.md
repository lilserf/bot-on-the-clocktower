# Bot on the Clocktower Changelog

## Version 2.0.1

* Fix crash when doing the `!character` command with no custom scripts registered

## Version 2.0.0

* **New feature:** `!voteTimer 5 minutes` to start a countdown timer that will perform the `!vote` command after 5 minutes.</br>Times can range from 15 seconds to 20 minutes.</br>Also has corresponding `!stopVoteTimer` command to cancel the timer, as well as shortcuts `!vt` and `!svt`.
* **New feature:** The bot can now look up characters with the `!character` command.</br>For custom characters, this requires some setup:</br>`!addScript <json url>` adds a custom script to the list of scripts the bot knows about on this server.
* Added `!setChatChannel <chat channel name>` command to support `!voteTimer`.
* The bot will now announce notable new versions (at our discretion, not every minor tiny change) to all registered servers

## Version 1.1.0

* The bot no longer **requires** the Night category. Run `!help` for `createTown` or `addTown` for details.

## Version 1.0.4

* Improved `!townInfo` output when used in an unrecognized channel
* Fixed exception when `!removeTown` is run with no params
* Optional MONGO_DB to allow for Dev DB separate from Prod

## Version 1.0.3

* Improved `!removeTown` error messages and made it flexible - either run in the control channel, or name the town
* Fixed index out of range in `!destroyTown`
* Fixed exception when `!addTown` is passed no parameters

## Version 1.0.2

* Fix bug in `!addTown` that would silently fail

## Version 1.0.1

* Cleaned up response messages for consistency and clarity

## Version 1.0.0

* Initial version with Setup and Gameplay command sets; see README.md for details

---

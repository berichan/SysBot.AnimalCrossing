# SysBot.AnimalCrossing
[SysBot.AnimalCrossing](https://github.com/kwsch/SysBot.AnimalCrossing) with the following extra functionality:

* Automatic offset handling for islands with multiple profiles.
* Automatic Dodo code retrieval & update from RAM whenever `$dodo` or `$code` is called. You may turn this off in the config and sudoers may update the code via `$fetchDodo`, otherwise `$overrideDodo`
* Injections will fill the entire inventory by default (you may change this in the config)
* Logs users arriving to your island with datestamps, and saves all logs to disk.

## Other Dependencies
Animal Crossing API logic is provided by [NHSE](https://github.com/kwsch/NHSE/).

# License
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)
Refer to the `License.md` for details regarding licensing.

﻿<div align="center">
    <img width="25%" src="Vulnerator_Logo_Large_Color_SquareBackground_20190805.png">
	<h1>Vulnerator</h1>
	<h2>Vulnerability Continuous Monitoring Made Easy</h2>
		<a href="https://gitter.im/Vulnerator/Vulnerator">
			<img src="https://img.shields.io/badge/Gitter-Join%20Chat-green.svg?style=flat-square&colorB=5523ed">
		</a>
        <a href="https://join.slack.com/t/vulnerator-chat/shared_invite/enQtMzQxMzc2MTE0NTI4LWQ1MTVmOGRmZjU4M2UzODU4ZDBhZDk1NGNlY2ZmMjgxNGEzNjUxMmE4OTkwNjQ3NTBhYzU3NmQ2OGI4YjViYzM">
			<img src="https://img.shields.io/badge/Slack-Join%20Chat-green.svg?style=flat-square&colorB=5523ed">
		</a>
		<a href="https://github.com/Vulnerator/Vulnerator/releases/latest">
			<img src="https://img.shields.io/github/release/Vulnerator/Vulnerator.svg?style=flat-square">
		</a>
		<a href="https://github.com/Vulnerator/Vulnerator/issues">
			<img src="https://img.shields.io/github/issues-raw/Vulnerator/Vulnerator.svg?style=flat-square">
		</a>
		<a href="https://github.com/Vulnerator/Vulnerator/issues">
			<img src="https://img.shields.io/github/issues-closed-raw/Vulnerator/Vulnerator.svg?style=flat-square&colorB=green">
		</a>
        <a href="https://github.com/Vulnerator/Vulnerator/blob/testing/LICENSE">
			<img src="https://img.shields.io/github/license/Vulnerator/Vulnerator.svg?style=flat-square&colorB=f77a1b">
		</a>
</div>

### Software Security
With the move from SoftwareForge to the public domain, the integrity of the application has recently been thrust into the limelight.  To ensure the application is secure, please note the following measures:
* [Alex Kuchta](https://github.com/amkuchta "Alex Kuchta's GitHub Page") has personally had his hand in every line of code in the application - there is not a single file that has not been touched, modified, or updated by him
* Only four GitHub users have the power to update the application.  This means that although anybody can fork the repository and change their personal repo, only one of the four "gatekeepers" can authorize a change to the ```master``` branch
* Each release is listed with both an ```MD5``` and ```SHA256``` checksum value - after you download the application, I encourage you to check the hash yourself to ensure that you downloaded what you expected
* If the above measures are not enough, please feel free to create your own fork of the repository and compile the application yourself - this will allow you to do a manual code review to ensure that no malicious lines exist before creating an executable.

### Helpful Links:
To get started, please check out (and bookmark!) the following locations - they are a treasure trove of knowledge (which I am told is power, and who doesn't want to be all-powerful?)!  

* **Vulnerator Project Page:** https://vulnerator.github.io/Vulnerator/
* **Software Releases:**  https://github.com/Vulnerator/Vulnerator/releases
* **Wiki Page(s):** https://github.com/Vulnerator/Vulnerator/wiki
* **Issue Tracker:** https://github.com/Vulnerator/Vulnerator/issues  
* **Change Log:** https://github.com/Vulnerator/Vulnerator/wiki/Change-Log  

### QuickStart Guide
Now that you have familiarized yourself with the available resources (you did click the links, didn't you?), jump in to using the software!  

1. Download the software from the [Releases](https://github.com/Vulnerator/Vulnerator/releases) page
  * **Note:** Chances are, unless you are a coder or interested in seeing "under the hood", you want the compiled release (the download without the word "Source" in it)
2. Extract the entire folder from the "*.zip" file you just downloaded
3. Launch the "Vulnerator.exe" file _from within the folder you just extracted_
  * The executable has hidden files that it depends on to run - they are shipped with the application.  If Vulnerator does not find these files in the directory it is in, it will yell at you, which will make you yell at me... and I don't like being yelled at.
4. Enjoy!

For a more detailed user guide, be sure to check out the [Wiki](https://github.com/Vulnerator/Vulnerator/wiki), and if you have any bugs or suggestions to report, post them on the [Issues](https://github.com/Vulnerator/Vulnerator/issues) page.

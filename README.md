#Project Description
A managed version of the cosign module for IIS 7 and above.

##About
[Cosign](http://cosign.sourceforge.net/) is an open-source project originally designed to provide the University of Michigan with a secure single sign-on web authentication system.

##Highlights
* [Cosign v3 client protocol](http://cosign.sourceforge.net/)
* Multiple Cosignd server support (DNS load balancing)
* Provides an alternative to the current cosign module using Managed code
* Administration module for IIS 7, no more editing config files with a text editor
* Added features like fall back to Basic Auth
* Thread safe, and 100% managed code

##Important Notes
* This version does not perform any sort of connection pooling, it will initiate a new connection to the cosign server each time it needs to verify

##Goals
* Continue to develop and update the module on a regular basis, providing fixes and enhancements
* Add ability to pass thru authentication using Keberos Tickets received from the cosign server
* Be able to handle proxied ip addresses more reliably
* Use a database for cookie storage
* Implement connection pooling



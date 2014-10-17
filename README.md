**2014-10-16:** This project is no longer being maintained.  It was designed to interface with version 1.2 and OwnCloud is now at version 7.

There are modern Windows, Mac, and Linux clients available from the OwnCloud website here: https://owncloud.com/products/desktop-clients/

~~OwnCloud (http://owncloud.org) Client~~

~~Code currently working for version 1.2 [http://owncloud.org/releases/owncloud-1.2.tar.bz2]~~

1. ~~Install OwnCloud version 1.2 per http://owncloud.org/index.php/Installation~~
2. ~~Create an OwnCloud user on your new site~~
3. ~~Edit owncloudclient.exe.config to create a unique EncryptionKey and InitilizationVector~~
4. ~~Run: owncloudclient.exe --owncloudurl=https://yoursite.com/path-to-owncloud~~
5. ~~See owncloudclient.exe --help for more options~~

~~Experimental work with mono(.net 3.5):~~
  - ~~Compile on windows and copy binaries to linux machine~~
  - ~~Copy the following two .net binaries from your windows machine:~~
    - ~~System.ServiceModel.dll~~
    - ~~System.Runtime.Serialization.dll~~
  - ~~Run via: mono OwnCloudClient.exe --owncloudurl=https://yoursite.com/path-to-owncloud~~

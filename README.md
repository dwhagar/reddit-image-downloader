# Reddit Image Downloader

This application is designed to be run from the command line as a scheduled task on Windows and I'm writing it to teach myself how to write in C@ rather than doing it in Python, which I already know. I got this idea from using IFTTT to download background images, and I wrote a python script that scanned new backgrounds and thew away stuff I knew I wouldn't want.

## Features

- Download images from specified subreddits and deposit files in a designated folder
- Keep track of what posts and images it has seen before to avoid duplicates
- Apply image saving criteria such as image size, aspect ratio, and brightness

## Roadmap

- Logging
- SQLite wrapper
- JSON wrapper
- File management
- Getting image properties
- Saving image properties for comparison (SQLite)
- Web requests
- CLI options
- Settings from file (JSON)
- Reddit access wrapper
- Saving post information (SQLite)
- GUI

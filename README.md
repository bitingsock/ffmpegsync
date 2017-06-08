# ffmpegsync
ffmpegSync is a directory mirroring application for media files. By setting a source directory, destination directory, and eligible input formats the program will copy all files in the source directory to the destination directory. Any file matching the file extensions listed in "inputformats" will be converted to the specified output format into the destination directory. When syncing is ever done afterwards, ffmpegSync will consider the files with specified output format to be equal to the source files.

The purpose of the conversion would typically be to have a smaller version of an otherwise too large library to copy to storage with limited space. Another, purpose could be simply that all files are required to be of a certain format in order to be useful. In any case, this allows you to keep the quality of your source files intact while having a different version of each file, accurately matching the original library of media.
**Requires:ffmpeg,ffprobe**

# USAGE:

ffmpegSync trys to read from a config file (named sync.config) in the directory from which it is run. If it is run with command line arguments they will overwrite any relevant option.
The valid options it looks for are as follows:

**purge=yes|no|only** *(optional)* (command line: -p,-purge)
Tells ffmpegSync if it should look for faulty files in the destination directory. Files are queried with ffprobe and will be deleted if they give any error whatsoever. Setting this option to "only" will purge faulty files then exit without copying any files to the destination. **(default = no)**

**source=DirectoryA** (command line: -s,-source)
The directory containing files to be copied to the new destination. If purge is not set to "only" this is a required setting.

**destination=DirectoryA** (command line: -d,-destination)
The directory to contain converted and copied files.

**inputformats=flac**(command line: -i, -inputformats)
A comma separated list of file extensions that ffmpeg will convert to the new format. If purge is not set to "only" this is a required setting.

**outputformat=opus** (command line: -o,-outputformat)
The file extension to be converted to. Keep in mind this only dictates the resulting file extension. ffmpeg automatically chooses an appropriate encoder for most file extensions, but if you require specific ffmpeg otions they can be set with the ffargs option. If purge is not set to "only" this is a required setting. **(default = opus)**

**bitrate=80000** (command line: -b,-bitrate)
The bitrate to encode at in bits per second. If purge is not set to "only" this is a required setting. **(default = 80000)**

__instances=__0 to 25
How many instances of ffprobe and ffmpeg to be able to run at once. Because many encoders are not multi-threaded in ffmpeg we can simply operate on several files at once to speed up the process. Setting this to 0 effectively pauses the scanning or converting loops. This setting can be modified during the sync using the ***up and down arrows*** **(default = 5)**

**ffargs=additional arguments** (command line: a string contained in "" e.g. "-c:v copy -compression_level 10")
Any specific settings to be passed to ffmpeg when converting. Technically speaking, because the argument parser considers spaces to delimit arguments, you actually only need to contain your string in quotes if it has spaces in it.

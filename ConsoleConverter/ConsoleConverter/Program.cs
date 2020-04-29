using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

namespace ConsoleConverter {
    internal class Program {
        protected const string fileSourcePath = "C:\\movies";
        protected const string fileMovePath = "C:\\DoneConvertedMovies";
        protected const string fileOutputPath = "C:\\ConvertedMovies";
        private static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            Queue<FileInfo> filesToConvert = new Queue<FileInfo>(GetFilesToConvert(fileSourcePath));
            await Console.Out.WriteLineAsync($"Find {filesToConvert.Count} files to convert.");

            //Set directory where the app should look for FFmpeg executables.
            FFmpeg.ExecutablesPath = Path.Combine("C:\\movies", "FFmpeg");
            ////Get latest version of FFmpeg. It's great idea if you don't know if you had installed FFmpeg.
            await FFmpeg.GetLatestVersion(FFmpegVersion.Full).ConfigureAwait(false);

            //Run conversion
            await RunConversion(filesToConvert).ConfigureAwait(false);

            Console.In.ReadLine();
        }

        private static async Task RunConversion(Queue<FileInfo> filesToConvert)
        {
            while (filesToConvert.TryDequeue(out FileInfo fileToConvert)) {
                //Save file to the same location with changed extension
                string outputFileName = Path.Combine(fileOutputPath, Path.ChangeExtension(fileToConvert.Name, FileExtensions.Ts));
                //string result = await Probe.New()
                //    .Start($"-loglevel error -skip_frame nokey -select_streams v:0 -show_entries frame=pkt_pts_time {fileToConvert}").ConfigureAwait(false);
                IMediaInfo mediaInfo = await MediaInfo.Get(fileToConvert).ConfigureAwait(false);
                var videoStream = mediaInfo.VideoStreams.First();
                var audioStream = mediaInfo.AudioStreams.First();

                ////Change some parameters of video stream
                //videoStream
                //    //Rotate video counter clockwise
                //    .Rotate(RotateDegrees.CounterClockwise)
                //    //Set size to 480p
                //    .SetSize(VideoSize.Hd480)
                //    //Set codec which will be used to encode file. If not set it's set automatically according to output file extension
                //    .SetCodec(VideoCodec.H264);

                //Create new conversion object
                var conversion = Conversion.New()
                    //Add video stream to output file
                    .AddStream(videoStream)
                    //Add audio stream to output file
                    .AddStream(audioStream)
                    //Set output file path
                    .SetOutput(outputFileName)
                    //SetOverwriteOutput to overwrite files. It's useful when we already run application before
                    .SetOverwriteOutput(true)
                    //Disable multithreading
                    .UseMultiThread(false)
                    //Set conversion preset. You have to chose between file size and quality of video and duration of conversion
                    .SetPreset(ConversionPreset.UltraFast);
                //Add log to OnProgress
                conversion.OnProgress += async (sender, args) => {
                    //Show all output from FFmpeg to console
                    await Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {fileToConvert.Name}").ConfigureAwait(false);
                };
                //Start conversion
                await conversion.Start().ConfigureAwait(false);

                await Console.Out.WriteLineAsync($"Finished conversion file [{fileToConvert.Name}]").ConfigureAwait(false);

                //await {
                //    if (!Directory.Exists(fileMovePath)) {
                //        Directory.CreateDirectory(fileMovePath);
                //    }
                //    string movePathMovies = Path.Combine(fileMovePath, fileToConvert.Name);
                //    File.Move(fileToConvert.FullName, movePathMovies);
                //    Console.Out.WriteLineAsync("Done moving finished converted movies");
                //};
                
            }
        }

        private static IEnumerable<FileInfo> GetFilesToConvert(string directoryPath)
        {
            //Return all files excluding mp4 because I want convert it to mp4
            return new DirectoryInfo(directoryPath).GetFiles();
        }
    }
}

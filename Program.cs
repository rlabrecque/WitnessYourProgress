using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

public static class BinaryReaderExtensionMethods {
    public static string ReadCString(this System.IO.BinaryReader reader) {
        List<byte> bytes = new List<byte>(128); // TODO(Riley): Clean all this up using ReadBytes();
        while (reader.PeekByte() != 0) {
            bytes.Add(reader.ReadByte());
        }
        reader.ReadByte();

        return System.Text.ASCIIEncoding.ASCII.GetString(bytes.ToArray());
    }


    // Peek functions are VERY expensive as they cause the stream to reseek
    public static byte PeekByte(this System.IO.BinaryReader reader) {
        byte ret = reader.ReadByte();
        reader.BaseStream.Position -= sizeof(byte);
        return ret;
    }

    public static int PeekInt32(this System.IO.BinaryReader reader) {
        int ret = reader.ReadByte();
        reader.BaseStream.Position -= sizeof(int);
        return ret;
    }
}

namespace WitnessYourProgress {
    class Program {
        //static string g_fileName = @"C:\Users\Riley\AppData\Roaming\The Witness\2016.01.26__time_20.22.49.witness_campaign"; // Blank fresh save
        //static string g_fileName = @"C:\Users\Riley\AppData\Roaming\The Witness\2016.01.27__time_01.45.23.witness_campaign"; // Moved forward, one puzzle completed
        static string g_fileName = @"C:\Users\Riley\AppData\Roaming\The Witness\2016.01.30__time_01.45.29.witness_campaign"; // Normal Save
        
        static void Main(string[] args) {
            Console.WriteLine("WitnessYourProgress v0.1 by Riley Labrecque");

            byte[] bytes = OpenSaveFile(g_fileName);
            if (bytes != null) {
                Console.WriteLine("Opened Save file successfully");
                Console.WriteLine("Size: " + bytes.Length);

                WitnessCampaignHeader header = ParseStateFromBytes(bytes);
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("version: " + header.version);
                Console.WriteLine("num_saves: " + header.num_saves);
                Console.WriteLine("num_game_wins: " + header.num_game_wins);
                Console.WriteLine("completed_this_game: " + header.completed_this_game);
                Console.WriteLine("misc_flags: " + header.misc_flags);
                Console.WriteLine("screen_size_setting: " + header.screen_size_setting);
                Console.WriteLine("num_panels_solved: " + header.num_panels_solved);
                Console.WriteLine("num_panels_total: " + header.num_panels_total);
                Console.WriteLine("num_envs_solved: " + header.num_envs_solved);
                Console.WriteLine("num_envs_total: " + header.num_envs_total);
                Console.WriteLine("num_obelisks_solved: " + header.num_obelisks_solved);
                Console.WriteLine("num_obelisks_total: " + header.num_obelisks_total);
                Console.WriteLine("show_subtitles: " + header.show_subtitles);
                Console.WriteLine("sound_effect_volume: " + header.sound_effect_volume);
                Console.WriteLine("music_volume: " + header.music_volume);
                Console.WriteLine("joystick_angle: " + header.joystick_angle);
                Console.WriteLine("time_of_save: " + header.time_of_save);
                Console.WriteLine("----------------------------------------------");

            }

            Console.ReadKey();
        }


        static byte[] OpenSaveFile(string fileName) {
            byte[] bytes = null;
            try {
                Console.WriteLine("Opening save file: " + fileName);
                bytes = File.ReadAllBytes(fileName);
            }
            catch (IOException e) {
                Console.WriteLine(e);
            }

            return bytes;
        }


        public class WitnessCampaignHeader {
            public int version;
            public int num_saves;
            public int num_game_wins;
            public int completed_this_game;
            public int misc_flags;
            public int screen_size_setting;
            public int num_panels_solved;
            public int num_panels_total;
            public int num_envs_solved;
            public int num_envs_total;
            public int num_obelisks_solved;
            public int num_obelisks_total;
            public int show_subtitles;
            public float sound_effect_volume;
            public float music_volume;
            public float joystick_angle;
            public DateTime time_of_save;
        }


        static WitnessCampaignHeader ParseStateFromBytes(byte[] bytes) {
            bool header_only = false;
            bool forced_fresh_return = false;

            WitnessCampaignHeader header = new WitnessCampaignHeader();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream)) {
                header.version = reader.ReadInt32();

                if (header.version > 17) {
                    Console.WriteLine("Versions > 17 are not supported yet!");
                    return null;
                }

                if (header.version >= 8) {
                    if (header.version >= 11) {
                        int unknown = reader.ReadInt32();
                        if (!header_only) {
                            if (unknown != 0) {
                                if (forced_fresh_return) {
                                    forced_fresh_return = true;
                                }
                            }
                        }
                    }
                }

                header.num_saves = reader.ReadInt32();
                header.num_game_wins = reader.ReadInt32();
                header.completed_this_game = reader.ReadInt32();
                header.misc_flags = reader.ReadInt32();

                header.screen_size_setting = reader.ReadByte();
                if (header.screen_size_setting < 0)
                    header.screen_size_setting = 0;
                if (header.screen_size_setting > 4)
                    header.screen_size_setting = 4;

                header.num_panels_solved = reader.ReadInt32();
                header.num_panels_total = reader.ReadInt32();
                if (header.version >= 15) {
                    header.num_envs_solved = reader.ReadInt32();
                    header.num_envs_total = reader.ReadInt32();
                    header.num_obelisks_solved = reader.ReadInt32();
                    header.num_obelisks_total = reader.ReadInt32();
                }
                header.show_subtitles = reader.ReadInt32();
                header.sound_effect_volume = reader.ReadSingle();
                header.music_volume = reader.ReadSingle();
                header.joystick_angle = reader.ReadSingle();

                header.time_of_save = DateTime.FromFileTime(reader.ReadInt64());

                if (!header_only) {
                    float theta = reader.ReadSingle();
                    float phi = reader.ReadSingle();
                    Console.WriteLine("theta: " + theta);
                    Console.WriteLine("phi: " + phi);

                    double time = reader.ReadDouble();
                    double dt = reader.ReadDouble();
                    Console.WriteLine("time: " + time);
                    Console.WriteLine("dt: " + dt);

                    make_entities_diffed(reader);

                }
            }

            return header;
        }

        static void make_entities_diffed(BinaryReader reader) {
            int file_version = reader.ReadInt32(); //unpack
            Console.WriteLine("file_version: " + file_version);

            load_type_manifest(reader, false, file_version);

            int unknownCount = reader.ReadInt32();
            Console.WriteLine("unknownCount2: " + unknownCount);

            /*for (int i = 0; i < unknownCount; ++i) {
                int unknownEntityId = reader.ReadInt32();
                Console.WriteLine("unknownEntityId: " + unknownEntityId);

                int unknownEntityOffset = reader.ReadByte();
                Console.WriteLine("unknownEntityOffset: " + unknownEntityOffset);

                int unknownCount2 = reader.ReadInt32();
                Console.WriteLine("unknownCount2: " + unknownCount2);
                for (int j = 0; j < unknownCount2; ++j) {
                    byte slot_index = reader.ReadByte();
                    Console.WriteLine("slot_index: " + slot_index);

                }

            }*/
        }

        static void load_type_manifest(BinaryReader reader, /*Auto_Array<Portable_Type_Load_Info*>* results,*/ bool load_as_text, int version) {
            Console.WriteLine("Bytes Left: " + (reader.BaseStream.Length - reader.BaseStream.Position));
            if ((reader.BaseStream.Length - reader.BaseStream.Position) >= 4) {
                if (load_as_text && version > 1) {
                    reader.ReadInt32();
                }
                else {
                    int unknown = reader.ReadInt32();
                    Console.WriteLine("unknown: " + unknown);
                }

                int unknowncount = reader.ReadInt32();
                Console.WriteLine("unknowncount: " + unknowncount);

                if (unknowncount <= 0) {
                    return;
                }

                for (int i = 0; i < unknowncount; ++i) {
                    string name = reader.ReadCString();
                    Console.WriteLine("name: " + name);
                    int revision_number = reader.ReadInt32();
                    Console.WriteLine("revision_number: " + revision_number);
                }
            }
        }
    }
}

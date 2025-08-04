using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;

namespace S7CommPlusGUIBrowser
{
    public static class S7AddressFormatter
    {
        /// <summary>
        /// Extracts and formats the classic S7 absolute address for a PlcTag.
        /// </summary>
        /// <param name="tag">The PlcTag to extract from.</param>
        /// <returns>Classic S7 address string, or empty if not available.</returns>
        public static string GetAbsoluteAddressString(PlcTag tag)
        {
            if (tag == null || tag.Address == null)
                return string.Empty;

            string area = null;
            int dbNumber = 0;
            int byteOffset = 0;
            int? bitOffset = null;
            uint accessArea = tag.Address.AccessArea;
            // TIA/1200/1500: Area1 = upper 16 bits, Area2 = next byte
            ushort area1 = (ushort)((accessArea & 0xFFFF0000) >> 16);
            byte area2 = (byte)((accessArea & 0x0000FF00) >> 8);
            // Classic S7: area code in upper byte
            byte classicArea = (byte)((accessArea & 0xFF000000) >> 24);

            // Area decoding (TIA and classic)
            if (area1 == 0x8A0E) // DB (TIA)
            {
                area = "DB";
            }
            else if (area1 == 0x0000) // I/Q/M/C/T (TIA)
            {
                switch (area2)
                {
                    case 0x50: area = "I"; break;
                    case 0x51: area = "Q"; break;
                    case 0x52: area = "M"; break;
                    case 0x53: area = "C"; break;
                    case 0x54: area = "T"; break;
                    default: area = "?"; break;
                }
            }
            else if (classicArea == 0x81) // Inputs (classic)
            {
                area = "I";
            }
            else if (classicArea == 0x82) // Outputs (classic)
            {
                area = "Q";
            }
            else if (classicArea == 0x83) // Merker (classic)
            {
                area = "M";
            }
            else if (classicArea == 0x84) // DB (classic)
            {
                area = "DB";
            }
            else if (classicArea == 0x1C || classicArea == 0x06) // Counter (classic)
            {
                area = "C";
            }
            else if (classicArea == 0x1D || classicArea == 0x05) // Timer (classic)
            {
                area = "T";
            }
            else
            {
                area = "?";
            }

            // Robust offset extraction for all S7 types
            if (area == "DB") {
                if (tag.Address.LID.Count > 0)
                    dbNumber = (int)tag.Address.LID[0];
                if (tag.Address.LID.Count > 1)
                    byteOffset = (int)tag.Address.LID[1];
                if (tag.Datatype == Softdatatype.S7COMMP_SOFTDATATYPE_BOOL && tag.Address.LID.Count > 2)
                    bitOffset = (int)tag.Address.LID[2];
            } else if (area == "I" || area == "Q" || area == "M") {
                if (tag.Address.LID.Count > 0)
                    byteOffset = (int)tag.Address.LID[0];
                if (tag.Datatype == Softdatatype.S7COMMP_SOFTDATATYPE_BOOL && tag.Address.LID.Count > 1)
                    bitOffset = (int)tag.Address.LID[1];
            } else if (area == "C" || area == "T") {
                if (tag.Address.LID.Count > 0)
                    byteOffset = (int)tag.Address.LID[0];
                if (tag.Datatype == Softdatatype.S7COMMP_SOFTDATATYPE_BOOL && tag.Address.LID.Count > 1)
                    bitOffset = (int)tag.Address.LID[1];
            }

            return FormatAddress(area, dbNumber, tag.Datatype, byteOffset, bitOffset);
        }

        /// <summary>
        /// Formats an S7 absolute address string for display in the GUI.
        /// </summary>
        /// <param name="area">Memory area: "DB", "M", "I", "Q"</param>
        /// <param name="dbNumber">DB number (for DB area), else 0</param>
        /// <param name="softdatatype">S7 softdatatype (uint, see Softdatatype.cs)</param>
        /// <param name="byteOffset">Byte offset</param>
        /// <param name="bitOffset">Bit offset (for BOOL), else null</param>
        /// <returns>Classic S7 address string</returns>
        public static string FormatAddress(string area, int dbNumber, uint softdatatype, int byteOffset, int? bitOffset = null)
        {
            // Robust area prefix for all S7 types
            string prefix;
            if (area == "DB")
                prefix = "DB" + dbNumber.ToString() + ".";
            else if (area == "I" || area == "Q" || area == "M")
                prefix = area;
            else if (area == "C")
                prefix = "C";
            else if (area == "T")
                prefix = "T";
            else
                prefix = area ?? "?";

            // Map datatype to S7 address format
            switch (softdatatype)
            {
                case Softdatatype.S7COMMP_SOFTDATATYPE_BOOL:
                    if (area == "DB")
                        return prefix + "DBX" + byteOffset + "." + (bitOffset ?? 0);
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset;
                    else
                        return prefix + byteOffset + "." + (bitOffset ?? 0);
                case Softdatatype.S7COMMP_SOFTDATATYPE_BYTE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_CHAR:
                case Softdatatype.S7COMMP_SOFTDATATYPE_USINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_SINT:
                    if (area == "DB")
                        return prefix + "DBB" + byteOffset;
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset;
                    else
                        return prefix + "B" + byteOffset;
                case Softdatatype.S7COMMP_SOFTDATATYPE_WORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WCHAR:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_INT:
                    if (area == "DB")
                        return prefix + "DBW" + byteOffset;
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset;
                    else
                        return prefix + "W" + byteOffset;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DWORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UDINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_REAL:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LWORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_ULINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LREAL:
                    if (area == "DB")
                        return prefix + "DBD" + byteOffset;
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset;
                    else
                        return prefix + "D" + byteOffset;
                case Softdatatype.S7COMMP_SOFTDATATYPE_STRING:
                    // Show start of string as DBB/MB/IB/QB
                    if (area == "DB")
                        return prefix + "DBB" + byteOffset + " (STRING)";
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset + " (STRING)";
                    else
                        return prefix + "B" + byteOffset + " (STRING)";
                case Softdatatype.S7COMMP_SOFTDATATYPE_WSTRING:
                    if (area == "DB")
                        return prefix + "DBW" + byteOffset + " (WSTRING)";
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset + " (WSTRING)";
                    else
                        return prefix + "W" + byteOffset + " (WSTRING)";
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY:
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTOD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LDT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DTL:
                    // Use word/dword address as appropriate
                    if (area == "DB")
                        return prefix + "DBD" + byteOffset + " (" + GetTypeName(softdatatype) + ")";
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset + " (" + GetTypeName(softdatatype) + ")";
                    else
                        return prefix + "D" + byteOffset + " (" + GetTypeName(softdatatype) + ")";
                default:
                    // Fallback: just area + offset
                    if (area == "DB")
                        return prefix + "DBB" + byteOffset + " (type " + softdatatype + ")";
                    else if (area == "C" || area == "T")
                        return prefix + byteOffset + " (type " + softdatatype + ")";
                    else
                        return prefix + "B" + byteOffset + " (type " + softdatatype + ")";
            }
        }

        /// <summary>
        /// Maps S7 softdatatype to a human-readable type name.
        /// </summary>
        /// <param name="softdatatype">S7 softdatatype (uint, see Softdatatype.cs)</param>
        /// <returns>Human-readable type name</returns>
        private static string GetTypeName(uint softdatatype)
        {
            // Add more mappings as needed
            switch (softdatatype)
            {
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATE: return "DATE";
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY: return "TIMEOFDAY";
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIME: return "TIME";
                case Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME: return "S5TIME";
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME: return "DATEANDTIME";
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTIME: return "LTIME";
                case Softdatatype.S7COMMP_SOFTDATATYPE_LTOD: return "LTOD";
                case Softdatatype.S7COMMP_SOFTDATATYPE_LDT: return "LDT";
                case Softdatatype.S7COMMP_SOFTDATATYPE_DTL: return "DTL";
                default: return $"{softdatatype}";
            }
        }
    }
}

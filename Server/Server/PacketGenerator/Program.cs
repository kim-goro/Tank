using System;
using System.IO;
using System.Xml;

namespace PacketGenerator
{
	/// <summary>
	/// 패킷 설계내용을 PDL.xml 적고
	/// main의 인자값으로 이를 전달해주면 자동으로 GenPackets.cs을 작성해주도록함
	/// 프로토콜 ID와 read(), write() 함수를 작성해줌
	/// </summary>
	class Program_Exam_
	{
		static string genPackets; // 패킷데이터를 분석하여 만들어진(채워진) 'GenPackets.cs'
		static ushort packetId;
		static string packetEnums;

		static string clientRegister;
		static string serverRegister;

		static void Main(string[] args) // *프로그램이 시작할때 인자를 넘겨줄 수 있음
		{
			string pdlPath = "../PDL.xml"; // 빌드파일(*.exe) 기준으로 PDL파일이 있는 상대경로

			XmlReaderSettings settings = new XmlReaderSettings()
			{
				IgnoreComments = true, // 주석무시
				IgnoreWhitespace = true // 공백무시
			};

			if (args.Length >= 1)
				pdlPath = args[0];

			using (XmlReader r = XmlReader.Create(pdlPath, settings)) // (xml파일 위치, 세팅값)
			{
				r.MoveToContent(); // 헤더를 건너뛰고 핵심 내용으로 바로 커서가 넘어감

				while (r.Read()) // 한줄한줄 string으로 읽어올 것임
				{
					if (r.Depth == 1 && r.NodeType == XmlNodeType.Element) // 'Depth == 1' => <packet~> 단계임, 'XmlNodeType.Element' => <packet~>의 시작하는 부분이다
						ParsePacket(r);
					//Console.WriteLine(r.Name + " " + r["name"]); // r.Name => type명, r["name"] => 변수명이 출력됨
				}

				// 파싱한 데이터를 *.cs로 만들어서 파일생성(자동화) 
				string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets); // => fileFormat + packetEnums + genPackets 템플릿 조립
				File.WriteAllText("GenPackets.cs", fileText);
				string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
				File.WriteAllText("ClientPacketManager.cs", clientManagerText);
				string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
				File.WriteAllText("ServerPacketManager.cs", serverManagerText);
			}
			// 마지막에 r.Dispose()를 해줘야하지만 using으로 영역을 구분할수도 있다
		}

		public static void ParsePacket(XmlReader r)
		{
			if (r.NodeType == XmlNodeType.EndElement) // 'XmlNodeType.EndElement' => </packet>의 끝나는 부분이 들어왔다? => return
				return;

			if (r.Name.ToLower() != "packet") // <packet>만 들여보냄
			{
				Console.WriteLine("Invalid packet node");
				return;
			}

			string packetName = r["name"]; // 올바른 데이터명만 들여보냄
			if (string.IsNullOrEmpty(packetName))
			{
				Console.WriteLine("Packet without name");
				return;
			}

			Tuple<string, string, string> t = ParseMembers(r);
			genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3); // PacketFormat_Exam_기본 템플릿의 {} 데이터들을 채워줌 => {0}packetName, {1}멤버변수들 선언코드, {2}멤버변수 Raad코드, {3}멤버변수 Write코드
			packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";

			// PacketManager
			if (packetName.StartsWith("S_") || packetName.StartsWith("s_")) // "S_Test"
				clientRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine; // ClientPacketManger에
			else // "C_PlayerInfoReq"
				serverRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine; // ServerPacketManager에
		}

		// {1} 멤버 변수들
		// {2} 멤버 변수 Read
		// {3} 멤버 변수 Write
		public static Tuple<string, string, string> ParseMembers(XmlReader r)
		{
			string packetName = r["name"];

			string memberCode = "";
			string readCode = "";
			string writeCode = "";

			int depth = r.Depth + 1;
			while (r.Read())
			{
				// Depth 단계를 뻉뻉이
				if (r.Depth != depth) // Depth를 역으로 간다면... => <Packet> 을 벗어났다는 의미 => 중단
					break;

				string memberName = r["name"]; // 어트리뷰트의 네임
				if (string.IsNullOrEmpty(memberName))
				{
					Console.WriteLine("Member without name");
					return null;
				}

				// XML을 한줄씩 읽어가며 멤버변수들 멤버코드, 리드코드, 라이트코드를 쭉 써주는 거임
				if (string.IsNullOrEmpty(memberCode) == false) // 이전에 작성했던 내용이 있다면 (= 이전 멤버변수꺼를 처리했다면) => 줄바꿈
					memberCode += Environment.NewLine;
				if (string.IsNullOrEmpty(readCode) == false)
					readCode += Environment.NewLine;
				if (string.IsNullOrEmpty(writeCode) == false)
					writeCode += Environment.NewLine;

				string memberType = r.Name.ToLower();
				switch (memberType) // 타입명으로 타입을 찾자!
				{
					case "byte":
					case "sbyte":
						memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
						readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
						writeCode += string.Format(PacketFormat.writeByteFormat, memberName, memberType);
						break;
					case "bool":
					case "short":
					case "ushort":
					case "int":
					case "long":
					case "float":
					case "double":
						memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
						readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
						writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
						break;
					case "string":
						memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
						readCode += string.Format(PacketFormat.readStringFormat, memberName);
						writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
						break;
					case "list":
						Tuple<string, string, string> t = ParseList(r);
						memberCode += t.Item1;
						readCode += t.Item2;
						writeCode += t.Item3;
						break;
					default:
						break;
				}
			}

			memberCode = memberCode.Replace("\n", "\n\t"); // 줄바꿈이 있는 위치에는 Tab해서 줄도 맞춰줌
			readCode = readCode.Replace("\n", "\n\t\t");
			writeCode = writeCode.Replace("\n", "\n\t\t");
			return new Tuple<string, string, string>(memberCode, readCode, writeCode);
		}

		public static Tuple<string, string, string> ParseList(XmlReader r)
		{
			string listName = r["name"];
			if (string.IsNullOrEmpty(listName))
			{
				Console.WriteLine("List without name");
				return null;
			}

			Tuple<string, string, string> t = ParseMembers(r);

			string memberCode = string.Format(PacketFormat.memberListFormat,
				FirstCharToUpper(listName),
				FirstCharToLower(listName),
				t.Item1,
				t.Item2,
				t.Item3);

			string readCode = string.Format(PacketFormat.readListFormat,
				FirstCharToUpper(listName),
				FirstCharToLower(listName));

			string writeCode = string.Format(PacketFormat.writeListFormat,
				FirstCharToUpper(listName),
				FirstCharToLower(listName));

			return new Tuple<string, string, string>(memberCode, readCode, writeCode);
		}

		public static string ToMemberType(string memberType)
		{
			switch (memberType)
			{
				case "bool":
					return "ToBoolean";
				case "short":
					return "ToInt16";
				case "ushort":
					return "ToUInt16";
				case "int":
					return "ToInt32";
				case "long":
					return "ToInt64";
				case "float":
					return "ToSingle";
				case "double":
					return "ToDouble";
				default:
					return "";
			}
		}

		public static string FirstCharToUpper(string input)
		{
			if (string.IsNullOrEmpty(input))
				return "";
			return input[0].ToString().ToUpper() + input.Substring(1);
		}

		public static string FirstCharToLower(string input)
		{
			if (string.IsNullOrEmpty(input))
				return "";
			return input[0].ToString().ToLower() + input.Substring(1);
		}
	}
}
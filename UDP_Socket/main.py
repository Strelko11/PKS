import socket

CLIENT_IP = "127.0.0.1"  # Client host IP
CLIENT_PORT = 50602  # Client port for receiving communication
SERVER_IP = "127.0.0.1"  # Server host IP
SERVER_PORT = 50601


class Client:

    def __init__(self, ip, port, server_ip, server_port) -> None:
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)  # UDP socket creation
        self.server_ip = server_ip
        self.server_port = server_port

    def receive(self):
        data, _ = self.sock.recvfrom(1024)  # buffer size is 1024 bytes
        return str(data, encoding="utf-8")

    def send_message(self, message):
        self.sock.sendto(bytes(message, encoding="utf8"), (self.server_ip, self.server_port))

    def quit(self):
        self.sock.close()  # correctly closing socket
        print("Client closed..")


if __name__ == "__main__":
    client = Client(CLIENT_IP, CLIENT_PORT, SERVER_IP, SERVER_PORT)
    data = "empty"

    while data != "End connection message received... closing connection":
        print("Input your message: ")
        client.send_message(input())
        data = client.receive()
        print(f"Server: {data}")

    client.quit()

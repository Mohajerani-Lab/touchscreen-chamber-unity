import socket
import threading

# Dictionary to map commands to functions
commands = {}

def command_handler(command):
    def decorator(func):
        commands[command] = func
        return func
    return decorator

@command_handler('greet')
def greet(data):
    return f"Hello, {data}!"

@command_handler('add')
def add(data):
    try:
        numbers = list(map(int, data.split()))
        return f"The sum is {sum(numbers)}"
    except ValueError:
        return "Error: Please provide a list of numbers."

@command_handler('exit')
def exit_server(data):
    return "exit"

def handle_client(client_socket):
    with client_socket:
        while True:
            try:
                message = client_socket.recv(1024).decode('utf-8')
                if not message:
                    break
                print(message)
                command, _, data = message.partition(' ')
                if command in commands:
                    response = commands[command](data.strip())
                    if response == "exit":
                        client_socket.sendall("Server shutting down...".encode('utf-8'))
                        server_socket.close()
                        break
                    client_socket.sendall(response.encode('utf-8'))
                else:
                    client_socket.sendall("Unknown command".encode('utf-8'))
            except ConnectionResetError:
                break

def start_server(host='0.0.0.0', port=65432):
    global server_socket
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((host, port))
    server_socket.listen()
    print(f"Server started on {host}:{port}")

    try:
        while True:
            client_socket, addr = server_socket.accept()
            print(f"Connection from {addr}")
            client_handler = threading.Thread(target=handle_client, args=(client_socket,))
            client_handler.start()
    except Exception as e:
        print(f"Error: {e}")
    finally:
        server_socket.close()

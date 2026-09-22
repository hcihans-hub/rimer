import ws from 'k6/ws';
import { sleep } from 'k6';

export const options = {
  vus: 2000,
  duration: '60s',
};

export default function () {
  ws.connect('ws://localhost:5000/ws', null, function (socket) {
    socket.on('open', () => {
      sleep(60);
    });
  });
}

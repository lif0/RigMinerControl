# RigMinerControl
Rig Miner Control/Управление Ригом
<img src="https://github.com/lif0/RigMinerControl/blob/master/Github/1.png"></img><br>
Bot commands/Функционал бота<br>
<img src="https://github.com/lif0/RigMinerControl/blob/master/Github/IMG_1.PNG"></img><br>

# <h3>Settings.ini - Settings/Настройки<h3>
<br>
[RigMinerSetting]<br>
RigName=RigName1 - Название рига(Их может быть несколько)<br>
CountRig=1 - Количество ригов<br>
thisRig=1 - Какой этот риг<br>
Miner=This miner from ethermine.org - Ссылку на статистику с сайта ethermine.org<br>
SpeedUpdateTemperature=1000 - Скорость обновления температуры<br>
UpdateSpeedCommand=1500- Скорость получение команд<br>
UpdateFailWait=5000 - Ожидание при ошибки(Отсутствие интернета)<br>
WaitingAllRig=1510- Ожидание остальных ригов<br>
language=English.ini - Язык интерфейса<br>
<br>
[Telegram]<br>
ChatsID=this people's chatID who control rig(Example:1233445,2134543)/ Чаты людей которые могут получать информацию о риге(Например:1233445,2134543)<br>
Token= Set u bot's token/ Укажите токен вашего бота<br>
LastUpdateID=0<br>
<br>
Set count GPU in u're rig/ Устанавливаем количество GPU на вашем риге<br>
<br>
[GPUAdapter.0]<br>
TemperatureLimit_NotifyMe=70 - Температура при которой вас начнут оповещать<br>
TemperatureLimit_Shutdown=83 - Температуры при которой рит выключится<br>
<br>
[GPUAdapter.1]<br>
TemperatureLimit_NotifyMe=70<br>
TemperatureLimit_Shutdown=83<br>
<br>
[GPUAdapter.2]<br>
TemperatureLimit_NotifyMe=70<br>
TemperatureLimit_Shutdown=83<br>
.......<br>
[GPUAdapter.N]<br>
TemperatureLimit_NotifyMe=70<br>
TemperatureLimit_Shutdown=83<br>

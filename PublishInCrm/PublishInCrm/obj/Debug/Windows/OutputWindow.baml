   M S B A M L   `   `   `  ����T  PMahApps.Metro, Version=1.1.2.0, Culture=neutral, PublicKeyToken=f4fb5a3c4d1e5b4fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls  5   	    PublishInCrm2   ,CemYabansu.PublishInCrm.Windows.OutputWindow   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   Pcontrols;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     5   	      AccentColorBrush#���   5   	   $��1q�5   	     � Title$  OUTPUT WINDOW��5   	   $	��274��6   $	��437��6,    � 
ResizeMode$ CanMinimize=�68    � 	Resources 5       �� MergedDictionaries �� Source�� 5	   
    5
      �� 5      $H Bpack://application:,,,/PublishInCrm;component/Resources/Icons.xaml?�6%   %    5      .��5      � $��
0,0,0,-180q�6   .+�5   
   �� $��Publishing Details��6   $
��Left=�6-   $��	10,10,0,0q�6H   $	��Top=�6[   $��37��6s   $	��211��6   $��20(�6�   $
��Bold"�6�   �� 5   
   -   5         ��Name$ ConnectionLabel��5      {�.  $��Connection ..��6;   $
��Left=�6S   $	��Top=�6n   $��	43,52,0,0q�6�   $	��387��6�   �� 5   
   -   5         ��Name$ ConnectionImage��5      $
��Left=�6)   $��24��6D   $��	10,52,0,0q�6P   $	��Top=�6c   $��24��6{   �� 5   
   -   5        $ GettingWebresourcesLabel��5      $��Getting webresoruces ..��62   {�.  $
��Left=�6f   $	��Top=�6�   $��	43,83,0,0q�6�   $	��387��6�   �� 5   
   -   5        $ GettingWebresourcesImage��5      $
��Left=�62   $��24��6M   $��	10,83,0,0q�6Y   $	��Top=�6l   $��24��6�   )   #MahApps.Metro.Controls.ProgressRing  5   
   -   5          Name$% GettingWebresourcesProgressRing��5          IsActive .  $��	10,83,0,0q�6Z   $��26��6m   $��26��6y   $
��Left=�6�   $	��Top=�6�   	   IsLarge	 .  6�   �� 5   
   -   5        $ CreateLabel��5      $*��$Creating non-existing webresources..��6%   {�.  $
��Left=�6f   $	��Top=�6�   $��
43,114,0,0q�6�   $	��387��6�   �� 5   
   -   5        $ CreateImage��5      $
��Left=�6%   $��24��6@   $��
10,114,0,0q�6L   $	��Top=�6`   $��24��6x     5   
   -   5        $ CreateProgressRing��5        .  $��
10,114,0,0q�6M   $��26��6a   $��26��6m   $
��Left=�6x   $	��Top=�6�   	 .  6�   �� 5   
   -	   5        $ UpdateLabel��5      $��Updating webresources..��6%   {�.  $
��Left=�6Y   $	��Top=�6t   $��
43,145,0,0q�6�   $	��387��6�   �� 5    
   -
   5        $ UpdateImage��5       $
��Left=�6%   $��24��6@   $��
10,145,0,0q�6L   $	��Top=�6`   $��24��6x     5!   
   -   5        $ UpdateProgressRing��5!        .  $��
10,145,0,0q�6M   $��26��6a   $��26��6m   $
��Left=�6x   $	��Top=�6�   	 .  6�   �� 5%   
   -   5        $ PublishLabel��5%      $��Publishing ..��6&   {�.  $
��Left=�6P   $	��Top=�6k   $��
43,176,0,0q�6�   $	��387��6�   �� 5&   
   -   5        $ PublishImage��5&      $
��Left=�6&   $��24��6A   $��
10,176,0,0q�6M   $	��Top=�6a   $��24��6y     5'   
   -   5        $ PublishProgressRing��5'        .  $��
10,176,0,0q�6N   $��26��6b   $��26��6n   $
��Left=�6y   $	��Top=�6�   	 .  6�   �� 5*   
   -   5        $ 
ErrorImage��5*      $
��Left=�6$   $��24��6?   $��	8,207,0,0q�6K   $	��Top=�6^   $��24��6v   $1��+/PublishInCrm;component/Resources/error.png��6�   $y�Hidden=�6�   �� 5+   
   -   5        $ 
ErrorLabel��5+      $�� ��6$   $
��Left=�6/   $	��Top=�6J   $��
41,207,0,0q�6b   $	��338��6v   $y�Visible=�6�   d� 5,   
   -   5        
 d�Name$
 ShowDetailsButton��5,      $
��Left=�63   $��379,192,0,0q�6N   $	��Top=�6c   $��50��6{   $��50��6�      MetroCircleToggleButtonStyle#���  5-      .��5.      �� $��20��6   $��20��5/      ��50      ��  �� Path$ 
Foreground� �� RelativeSource �� *FindAncestor+ �� AncestorType" d� J� OpacityMask 51      !� 52       j� Stretch$
 Fill=�6"     appbar_arrow_down !� Visual# [ 53      55      56      �� 57   
   -   5         ��Name$ OutputTextBox��57      $y�Hidden=�6)   $
��Left=�6=   $	��161��6X   $��
10,252,0,0q�6e    �� TextWrapping$
 Wrap=�6y   $	��Top=�6�   $	��415��6�   58      59      